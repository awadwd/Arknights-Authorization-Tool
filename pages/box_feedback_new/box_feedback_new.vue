<template>

		<view class="container" :class="'theme-' + themeMode">
		<!-- 头部信息 -->
		<view class="header">
			<text class="page-title">数据反馈</text>
			<text class="box-id" v-if="boxId">盒号: {{ boxId }}</text>
		</view>

		<!-- 反馈说明 -->
		<view class="notice-section">
			<view class="notice-icon">📢</view>
			<view class="notice-content">
				<text class="notice-title">反馈须知</text>
				<text class="notice-text">• 请确保反馈内容真实准确</text>
				<text class="notice-text">• 反馈将在管理员审核后发布</text>
				<text class="notice-text">• 请勾选需要修改的字段并填写正确数据</text>
			</view>
		</view>

		<!-- 反馈表单 -->
		<view class="form-section">
			<text class="section-title">反馈信息</text>
			
			<!-- 盒号 -->
			<view class="form-item">
				<text class="form-label required">盒号</text>
				<input class="form-input" v-model="feedbackForm.boxId" placeholder="请输入盒号，如 1.0" :disabled="!!boxId"/>
			</view>

			<!-- 反馈类型 -->
			<view class="form-item">
				<text class="form-label required">反馈类型</text>
				<picker class="form-picker" @change="onTypeChange" :value="typeIndex" :range="feedbackTypes">
					<view class="picker-text">{{ feedbackTypes[typeIndex] }}</view>
				</picker>
			</view>

			<!-- 数据修改区域 -->
			<view class="modify-section" v-if="typeIndex === 0">
				<text class="subsection-title">请勾选需要修改的字段</text>
				
				<view class="field-modify-item">
					<view class="field-header">
						<checkbox :value="'release_date'" :checked="selectedFields.includes('release_date')" @click="toggleField('release_date')" color="#409EFF"/>
						<text class="field-label">发售日期</text>
					</view>
					<input class="field-input" v-model="fieldData.release_date" placeholder="如：2022/10/25" :disabled="!selectedFields.includes('release_date')"/>
				</view>

				<view class="field-modify-item">
					<view class="field-header">
						<checkbox :value="'retail_price'" :checked="selectedFields.includes('retail_price')" @click="toggleField('retail_price')" color="#409EFF"/>
						<text class="field-label">零售价格</text>
					</view>
					<input class="field-input" v-model="fieldData.retail_price" placeholder="如：99元" :disabled="!selectedFields.includes('retail_price')"/>
				</view>

				<view class="field-modify-item">
					<view class="field-header">
						<checkbox :value="'size_material'" :checked="selectedFields.includes('size_material')" @click="toggleField('size_material')" color="#409EFF"/>
						<text class="field-label">尺寸、材质</text>
					</view>
					<input class="field-input" v-model="fieldData.size_material" placeholder="如：W90*D30*H140mm，PVC、ABS" :disabled="!selectedFields.includes('size_material')"/>
				</view>

				<view class="field-modify-item">
					<view class="field-header">
						<checkbox :value="'type'" :checked="selectedFields.includes('type')" @click="toggleField('type')" color="#409EFF"/>
						<text class="field-label">类型</text>
					</view>
					<picker class="field-picker" @change="onTypeFieldChange" :value="typeFieldIndex" :range="typeOptions" :disabled="!selectedFields.includes('type')">
						<view class="picker-text">{{ typeOptions[typeFieldIndex] }}</view>
					</picker>
				</view>

				<view class="field-modify-item">
					<view class="field-header">
						<checkbox :value="'replicate'" :checked="selectedFields.includes('replicate')" @click="toggleField('replicate')" color="#409EFF"/>
						<text class="field-label">复刻状态</text>
					</view>
					<picker class="field-picker" @change="onReplicateChange" :value="replicateIndex" :range="replicateOptions" :disabled="!selectedFields.includes('replicate')">
						<view class="picker-text">{{ replicateOptions[replicateIndex] }}</view>
					</picker>
				</view>
				
				<!-- 复刻时间 -->
				<view class="field-modify-item">
					<view class="field-header">
						<text class="field-label">复刻时间</text>
					</view>
					<input class="field-input" v-model="fieldData.replicate_date" placeholder="如：2026/5/29"/>
				</view>

				<!-- <view class="field-modify-item">
					<view class="field-header">
						<checkbox :value="'characters'" :checked="selectedFields.includes('characters')" @click="toggleField('characters')" color="#409EFF"/>
						<text class="field-label">干员列表</text>
					</view>
					<textarea class="field-textarea" v-model="fieldData.characters" placeholder="请输入干员名称，用逗号分隔，如：阿米娅, 德克萨斯, 拉普兰德" :disabled="!selectedFields.includes('characters')" :maxlength="500"/>
				</view> -->
			</view>
			
			<!-- 干员列表（市价反馈上方） -->
			<view class="form-item market-char-section" v-if="typeIndex === 0">
				<view class="section-title">干员列表</view>
				<view class="market-price-tip">当前盒子包含以下干员，可直接用于市价反馈</view>
				<view class="character-chips" v-if="boxCharacters && boxCharacters.length > 0">
					<view
						class="character-chip"
						v-for="char in boxCharacters"
						:key="char"
						:class="{ 'selected': selectedPriceChars.includes(char) }"
						@click="togglePriceChar(char)"
					>
						{{ char }}
					</view>
				</view>
				<view class="market-price-tip" v-else>当前盒子暂无干员数据</view>
			</view>

			<!-- 市价信息反馈 -->
			<view class="form-item character-price-section" v-if="typeIndex === 0">
				<view class="section-title">市价信息反馈</view>

				<!-- 手动新增干员 -->
				<view class="custom-char-row">
					<input class="custom-char-input" v-model="newCharInput" placeholder="输入干员名称"/>
					<view class="custom-char-add-btn" @click="addCustomChar">
						<text>+ 添加</text>
					</view>
				</view>

				<!-- 已选干员的市价填写区 -->
				<view class="price-input-area" v-if="selectedPriceChars.length > 0">
					<view class="price-char-item" v-for="char in selectedPriceChars" :key="char">
						<view class="price-char-header">
							<text class="price-char-name">{{ char }}</text>
							<text class="price-char-remove" @click="removePriceChar(char)">移除</text>
						</view>
						<view class="price-inputs-row">
							<view class="price-input-group">
								<text class="price-input-label">精一</text>
								<input class="price-input" type="number" v-model="priceData[char].ELITE1" placeholder="如：165"/>
							</view>
							<view class="price-input-group">
								<text class="price-input-label">精二</text>
								<input class="price-input" type="number" v-model="priceData[char].ELITE2" placeholder="如：170"/>
							</view>
						</view>
					</view>
				</view>
				<view class="market-price-tip" v-else>请从上方干员列表选择，或手动添加干员后填写价格</view>
			</view>

			<!-- 参考资料 -->
			<view class="form-item reference-section">
				<text class="form-label required">参考资料（最多10份，必填）</text>
				<text class="form-hint">支持图片/文字/链接，支持官方渠道和第三方平台，目前上传图片暂时无法审核</text>
				<text class="form-warning">⚠️ 未填写参考资料将直接被拒</text>
				
				<view class="reference-list">
					<view class="reference-item" v-for="(ref, index) in references" :key="index">
						<view class="reference-header">
							<text class="reference-type">{{ getRefTypeText(ref.type) }}</text>
							<view class="reference-delete" @tap="removeReference(index)">×</view>
						</view>
						<image v-if="ref.type === 'image'" :src="ref.content" mode="aspectFill" class="reference-image" @tap="previewReferenceImage(index)"></image>
						<text v-if="ref.type === 'text'" class="reference-text">{{ ref.content }}</text>
						<text v-if="ref.type === 'link'" class="reference-link" @tap="openLink(ref.content)">{{ ref.content }}</text>
					</view>
				</view>
				
				<view class="add-reference-btn" @tap="showRefTypeSelector" v-if="references.length < 10">
					<text class="add-icon">+</text>
					<text class="add-text">添加参考资料</text>
				</view>
			</view>

			<view class="reference-notice">
				<text class="notice-text">* 仅支持：明日方舟官方淘宝旗舰店、明日方舟朝陇山官方微博/抖音/森空岛账号、明日方舟官方微博及千岛等第三方app记录的数据或者文章信息</text>
			</view>

			<!-- 详细说明 -->
			<view class="form-item">
				<text class="form-label">详细说明（可选）</text>
				<textarea class="form-textarea" v-model="feedbackForm.description" placeholder="请详细描述问题或提供其他补充信息" :maxlength="500"/>
			</view>

			<!-- 联系方式 -->
			<view class="form-item">
				<text class="form-label">联系方式（可选）</text>
				<input class="form-input" v-model="feedbackForm.contact" placeholder="QQ/微信/邮箱，便于我们联系您确认信息"/>
			</view>

			<!-- 隐私政策 -->
			<view class="privacy-section">
				<checkbox :value="'agree'" :checked="privacyAgreed" @click="togglePrivacyAgreement" color="#409EFF"/>
				<text class="privacy-text">我已阅读并同意</text>
				<text class="privacy-link" @tap="showPrivacyPolicy">《隐私政策》</text>
			</view>
		</view>

		<!-- 提交按钮 -->
		<view class="submit-section">
			<button class="submit-btn" :disabled="!isFormValid || submitting" @click="submitFeedback">
				{{ submitting ? '提交中...' : '提交反馈' }}
			</button>
			<text class="submit-hint">提交后将在1-3个工作日内审核</text>
		</view>

		<!-- 提交成功提示 -->
		<view class="success-modal" v-if="showSuccess">
			<view class="success-content">
				<text class="success-icon">✓</text>
				<text class="success-title">提交成功</text>
				<text class="success-text">感谢您的反馈，我们会尽快处理</text>
				<button class="success-btn" @tap="closeSuccess">好的</button>
			</view>
		</view>

		<!-- 隐私政策模态框 -->
		<view class="modal-overlay" v-if="showPrivacyModal" @tap="closePrivacyPolicy">
			<view class="modal-content" @tap.stop>
				<text class="modal-title">隐私政策</text>
				<scroll-view class="modal-body" scroll-y>
					<text class="modal-text">感谢您使用方舟通行证谷子查询工具。我们收集的信息仅用于审核反馈，不会分享给第三方。

1. 收集的信息：盒号、修改数据、参考资料、联系方式（选填）。

2. 信息的使用：审核反馈、改进数据质量、必要时联系您。

3. 联系方式：通过GitHub仓库提交Issue。

感谢您的信任！</text>
				</scroll-view>
				<button class="modal-btn" @tap="closePrivacyPolicy">关闭</button>
			</view>
		</view>
	</view>
</template>

<script>
	import errorLog from "@/utils/errorLog.js";
	export default {
		data() {
			return {
				themeMode: 'simple',
				boxId: '',
				submitting: false,
				showSuccess: false,
				privacyAgreed: false,
				showPrivacyModal: false,
				typeIndex: 0,
				feedbackTypes: ['数据错误', '数据缺失', '新增数据', '其他问题'],
				selectedFields: [],
				boxCharacters: [],
				selectedPriceChars: [],
				priceData: {},
				newCharInput: '',  // 手动新增干员输入框
				fieldData: {
					release_date: '',
					retail_price: '',
					size_material: '',
					type: '',
					replicate: '',
					replicate_date: '',
					replicate: '',
					characters: ''
				},
				typeFieldIndex: 0,
				typeOptions: ['盲抽', '单领/赠品','其他'],
				replicateIndex: 0,
				replicateOptions: ['未复刻', '已复刻', '待定'],
				references: [],
				feedbackForm: {
					boxId: '',
					description: '',
					contact: ''
				}
			}
		},
		
		computed: {
			isFormValid() {
				if (!this.privacyAgreed) return false;
				if (!this.feedbackForm.boxId.trim()) return false;
				if (this.typeIndex === 0 && this.selectedFields.length === 0) return false;
				if (this.typeIndex === 0) {
					for (let field of this.selectedFields) {
						if (this.fieldData[field] && this.fieldData[field].trim()) {
							return true;
						}
					}
					return false;
				}
				return true;
			}
		},
		
		onLoad(options) {
			this.loadThemeSetting();
			this.boxId = options.boxId || '';
			if (this.boxId) {
				this.feedbackForm.boxId = this.boxId;
				this.loadBoxData(this.boxId);
				this.loadBoxCharacters();
			}
			uni.setNavigationBarTitle({ title: '数据反馈' });
		},
		
		methods: {
			logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
			loadThemeSetting() {
				try {
					const themeMode = uni.getStorageSync('themeMode');
					if (themeMode && (themeMode === 'simple' || themeMode === 'dark' || themeMode === 'jieyuan')) {
						this.themeMode = themeMode;
					}
				} catch (e) {
					this.logError(e);
					console.error('加载主题设置失败:', e); 
				}
			},
			
			loadBoxData(boxId) {
				try {
					console.log('=== 开始加载盒号数据 ===');
					console.log('目标盒号:', boxId);
					
					// 修正：键名是 arknightsData（有 s）
					let arknightData = uni.getStorageSync('arknightsData');
					console.log('arknightsData 直接读取:', arknightData ? '有数据' : '无数据');
					
					if (!arknightData) {
						const dataString = uni.getStorageSync('arknightsDataString');
						console.log('arknightsDataString:', dataString ? '有数据' : '无数据');
						if (dataString) { 
							arknightData = JSON.parse(dataString);
							console.log('解析后数据量:', arknightData ? arknightData.length : 0);
						}
					}
					
					if (arknightData && Array.isArray(arknightData)) {
						console.log('总数据条数:', arknightData.length);
						// 尝试多种可能的键名
						let boxData = arknightData.find(box => box.Box_id === boxId);
						if (!boxData) {
							boxData = arknightData.find(box => box.boxId === boxId);
						}
						if (!boxData) {
							boxData = arknightData.find(box => box.id === boxId);
						}
						console.log('找到的盒号数据:', boxData);
						if (boxData) { 
							this.prefillFormWithBoxData(boxData);
						} else {
							console.warn('未找到盒号:', boxId);
							console.log('第一条数据示例:', JSON.stringify(arknightData[0]).substring(0, 200));
						}
					} else {
						console.error('数据格式错误，不是数组');
					}
				} catch (e) { 
					this.logError(e);
					console.error('加载盒号数据失败:', e);
				}
			},
			
			prefillFormWithBoxData(boxData) {
				console.log('=== 开始填充表单数据 ===');
				console.log('接收到的 boxData:', JSON.stringify(boxData, null, 2));
				
				// 填充数据（不自动勾选，让用户手动选择需要修改的字段）
				if (boxData.release_date) {
					console.log('填充 release_date:', boxData.release_date);
					this.fieldData.release_date = boxData.release_date;
				}
				if (boxData.retail_price) {
					console.log('填充 retail_price:', boxData.retail_price);
					this.fieldData.retail_price = boxData.retail_price;
				}
				if (boxData.size || boxData.material) {
					let sizeMaterial = '';
					if (boxData.size) sizeMaterial += boxData.size;
					if (boxData.material) sizeMaterial += (sizeMaterial ? '，' : '') + boxData.material;
					this.fieldData.size_material = sizeMaterial;
				}
				// type 字段："true"=盲抽，"false"=单领/赠品，其他=其他
				if (boxData.type !== undefined) {
					if (boxData.type === true || boxData.type === 'true') {
						this.typeFieldIndex = 0; // 盲抽
						this.fieldData.type = '盲盒';
					} else if (boxData.type === false || boxData.type === 'false') {
						this.typeFieldIndex = 1; // 单领/赠品
						this.fieldData.type = '单领/赠品';
					} else {
						this.typeFieldIndex = 2; // 其他
						this.fieldData.type = '其他';
					}
				}
				// replicate 字段：复刻状态
				if (boxData.replicate !== undefined) {
					this.replicateIndex = (boxData.replicate === true || boxData.replicate === 'true') ? 1 : 0;
					this.fieldData.replicate = this.replicateOptions[this.replicateIndex];
				}
				const characters = [];
				for (let i = 1; i <= 10; i++) {
					const charKey = 'character' + i;
					if (boxData[charKey]) {
						if (typeof boxData[charKey] === 'string') {
							characters.push(boxData[charKey]);
						} else if (boxData[charKey].name) {
							characters.push(boxData[charKey].name);
						}
					}
				}
				if (characters.length > 0) { 
					this.fieldData.characters = characters.join(', ');
				}
				uni.showToast({ title: '已加载当前数据，请勾选需要修改的字段', icon: 'none', duration: 3000 });
			},
			
			onTypeChange(e) { this.typeIndex = e.detail.value; },
			
			toggleField(field) {
				const index = this.selectedFields.indexOf(field);
				if (index > -1) { this.selectedFields.splice(index, 1); }
				else { this.selectedFields.push(field); }
			},
			
			onTypeFieldChange(e) {
				this.typeFieldIndex = e.detail.value;
				this.fieldData.type = this.typeOptions[this.typeFieldIndex];
			},
			
			onReplicateChange(e) {
				this.replicateIndex = e.detail.value;
				this.fieldData.replicate = this.replicateOptions[this.replicateIndex];
			},
			
			togglePriceChar(char) {
				const index = this.selectedPriceChars.indexOf(char);
				if (index > -1) {
					this.selectedPriceChars.splice(index, 1);
					delete this.priceData[char];
				} else {
					this.selectedPriceChars.push(char);
					this.$set(this.priceData, char, { ELITE1: '', ELITE2: '' });
				}
			},
			
			removePriceChar(char) {
				const index = this.selectedPriceChars.indexOf(char);
				if (index > -1) {
					this.selectedPriceChars.splice(index, 1);
					delete this.priceData[char];
				}
			},
			
			loadBoxCharacters() {
				try {
					const data = uni.getStorageSync('arknightsData') || {};
					const boxId = this.feedbackForm.boxId;
					let box = null;
					if (Array.isArray(data)) {
						box = data.find(b => b.Box_id === boxId || b.boxId === boxId || b.id === boxId);
					} else {
						for (let key in data) {
							if (data[key].Box_id === boxId || data[key].boxId === boxId || data[key].id === boxId) {
								box = data[key];
								break;
							}
						}
					}
					if (box) {
						const chars = [];
						for (let i = 1; i <= 10; i++) {
							const charKey = 'character' + i;
							if (box[charKey]) {
								if (typeof box[charKey] === 'string') {
									chars.push(box[charKey]);
								} else if (box[charKey].name) {
									chars.push(box[charKey].name);
								}
							}
						}
						if (chars.length === 0 && Array.isArray(box.characters)) {
							this.boxCharacters = box.characters.map(c => typeof c === 'string' ? c : (c.name || ''));
						} else if (chars.length > 0) {
							this.boxCharacters = chars;
						}
					}
				} catch (e) {
					this.logError(e);
					console.log('loadBoxCharacters error', e);
				}
			},

			togglePriceChar(char) {
				const index = this.selectedPriceChars.indexOf(char);
				if (index > -1) {
					this.selectedPriceChars.splice(index, 1);
					delete this.priceData[char];
				} else {
					this.selectedPriceChars.push(char);
					this.$set(this.priceData, char, { ELITE1: '', ELITE2: '' });
				}
			},

			removePriceChar(char) {
				const index = this.selectedPriceChars.indexOf(char);
				if (index > -1) {
					this.selectedPriceChars.splice(index, 1);
					delete this.priceData[char];
				}
			},

			addCustomChar() {
				const name = this.newCharInput.trim();
				if (!name) return;
				if (this.selectedPriceChars.includes(name)) {
					uni.showToast({ title: '该干员已在列表中', icon: 'none' });
					return;
				}
				this.selectedPriceChars.push(name);
				this.$set(this.priceData, name, { ELITE1: '', ELITE2: '' });
				this.newCharInput = '';
			},

			showRefTypeSelector() {
				uni.showActionSheet({
					itemList: ['图片', '文字', '链接'],
					success: (res) => {
						const typeMap = ['image', 'text', 'link'];
						this.addReference(typeMap[res.tapIndex]);
					}
				});
			},
			
			addReference(type) {
				if (type === 'image') {
					// #ifdef MP-WEIXIN
					wx.requirePrivacyAuthorize({
						success: () => { this.chooseImageRef(); },
						fail: () => { this.onPrivacyCancel(); }
					});
					// #endif
					// #ifndef MP-WEIXIN
					this.chooseImageRef();
					// #endif
				} else if (type === 'text') {
					uni.showModal({
						title: '输入文字说明', editable: true, placeholderText: '请输入参考资料说明文字',
						success: (res) => {
							if (res.confirm && res.content) {
								this.references.push({ type: 'text', content: res.content });
							}
						}
					});
				} else if (type === 'link') {
					uni.showModal({
						title: '输入链接', editable: true, placeholderText: '请输入参考资料链接',
						success: (res) => {
							if (res.confirm && res.content) {
								this.references.push({ type: 'link', content: res.content });
							}
						}
					});
				}
			},
			
			chooseImageRef() {
				uni.chooseImage({
					count: 1, sizeType: ['compressed'], sourceType: ['album', 'camera'],
					success: (res) => {
						this.references.push({ type: 'image', content: res.tempFilePaths[0] });
					}
				});
			},

			onPrivacyConfirm() {
				this.showPrivacyModal = false;
				if (this.pendingRefType) {
					const t = this.pendingRefType;
					this.pendingRefType = '';
					if (t === 'image') this.chooseImageRef();
				}
			},

			onPrivacyCancel() {
				this.showPrivacyModal = false;
				this.pendingRefType = '';
				uni.showToast({ title: '需要同意隐私协议才能使用摄像头', icon: 'none' });
			},

			removeReference(index) { this.references.splice(index, 1); },
			
			previewReferenceImage(index) {
				const images = this.references.filter(ref => ref.type === 'image').map(ref => ref.content);
				const current = this.references[index].content;
				if (images.length > 0) { uni.previewImage({ urls: images, current: current }); }
			},
			
			openLink(url) {
				uni.setClipboardData({
					data: url,
					success: () => { uni.showToast({ title: '链接已复制', icon: 'success' }); }
				});
			},
			
			getRefTypeText(type) {
				const typeMap = { 'image': '图片', 'text': '文字', 'link': '链接' };
				return typeMap[type] || '未知';
			},
			
			togglePrivacyAgreement() { this.privacyAgreed = !this.privacyAgreed; },
			showPrivacyPolicy() { this.showPrivacyModal = true; },
			closePrivacyPolicy() { this.showPrivacyModal = false; },
			
			async submitFeedback() {
				if (!this.isFormValid || this.submitting) return;
				this.submitting = true;
				try {
					let modifyData = {};
					for (let field of this.selectedFields) {
						if (this.fieldData[field]) { modifyData[field] = this.fieldData[field]; }
					}
					const feedbackData = {
						boxId: this.feedbackForm.boxId,
						type: this.feedbackTypes[this.typeIndex],
						modifyData: modifyData,
						references: this.references,
						description: this.feedbackForm.description,
						contact: this.feedbackForm.contact,
						status: 'pending',
						createTime: new Date().toISOString(),
						privacyAgreed: this.privacyAgreed
					};
					let apiSuccess = false;
					try {
						const response = await uni.request({
							url: 'https://arknightsauthorizationseriesdata-editor.pages.dev/api/feedback',
							method: 'POST', data: feedbackData, timeout: 10000
						});
						if (response.statusCode === 200) {
							apiSuccess = true;
							console.log('API提交成功:', response.data);
						}
					} catch (apiError) {
						this.logError(apiError);
						console.error('API提交失败，将保存到本地:', apiError);
					}
					if (!apiSuccess) {
						let feedbackList = [];
						try {
							const stored = uni.getStorageSync('feedbackList');
							if (stored) { feedbackList = JSON.parse(stored); }
						} catch (e) {
							this.logError(e);
							console.error('读取反馈列表失败:', e); 
						}
						feedbackList.push(feedbackData);
						uni.setStorageSync('feedbackList', JSON.stringify(feedbackList));
						uni.showToast({ title: '网络异常，已保存到本地', icon: 'none' });
					}
					this.showSuccess = true;
				} catch (error) {
					this.logError(error);
					console.error('提交反馈失败:', error);
					uni.showToast({ title: '提交失败，请重试', icon: 'none' });
				} finally {
					this.submitting = false;
				}
			},
			
			closeSuccess() {
				this.showSuccess = false;
				uni.navigateBack();
			}
		}
	}
</script>

<style>
	.container { padding: 30rpx; min-height: 100vh; background-color: #f5f5f5; }
	.header { margin-bottom: 30rpx; }
	.page-title { font-size: 40rpx; font-weight: bold; color: #333; display: block; margin-bottom: 10rpx; }
	.box-id { font-size: 28rpx; color: #409EFF; font-weight: 500; }
	.notice-section { display: flex; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 16rpx; padding: 30rpx; margin-bottom: 30rpx; box-shadow: 0 4rpx 20rpx rgba(102, 126, 234, 0.3); }
	.notice-icon { font-size: 48rpx; margin-right: 20rpx; }
	.notice-content { flex: 1; }
	.notice-title { font-size: 30rpx; font-weight: bold; color: #fff; display: block; margin-bottom: 10rpx; }
	.notice-text { font-size: 24rpx; color: rgba(255, 255, 255, 0.9); display: block; line-height: 1.6; }
	.form-section { background-color: #fff; border-radius: 16rpx; padding: 30rpx; box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05); }
	.section-title { font-size: 32rpx; font-weight: bold; color: #333; display: block; margin-bottom: 30rpx; padding-bottom: 20rpx; border-bottom: 2rpx solid #f0f0f0; }
	.subsection-title { font-size: 28rpx; font-weight: bold; color: #409EFF; display: block; margin-bottom: 20rpx; }
	.form-item { margin-bottom: 30rpx; }
	.form-label { font-size: 28rpx; color: #333; font-weight: 500; display: block; margin-bottom: 15rpx; }
	.form-label.required::before { content: '* '; color: #f56c6c; margin-right: 8rpx; }
	.form-input { width: 100%; height: 80rpx; background-color: #f8f8f8; border-radius: 10rpx; padding: 0 20rpx; font-size: 28rpx; color: #333; box-sizing: border-box; }
	.form-picker { width: 100%; height: 80rpx; background-color: #f8f8f8; border-radius: 10rpx; padding: 0 20rpx; display: flex; align-items: center; }
	.picker-text { font-size: 28rpx; color: #333; }
	.form-textarea { width: 100%; min-height: 150rpx; background-color: #f8f8f8; border-radius: 10rpx; padding: 20rpx; font-size: 28rpx; color: #333; box-sizing: border-box; }
	.modify-section { background-color: #f8fafc; border-radius: 12rpx; padding: 20rpx; margin-bottom: 30rpx; }
	.field-modify-item { margin-bottom: 25rpx; padding-bottom: 25rpx; border-bottom: 1rpx solid #e0e0e0; }
	.field-modify-item:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }
	.field-header { display: flex; align-items: center; margin-bottom: 15rpx; }
	.field-label { font-size: 28rpx; color: #333; font-weight: 500; margin-left: 10rpx; }
	.field-input { width: 100%; height: 70rpx; background-color: #fff; border-radius: 8rpx; padding: 0 20rpx; font-size: 26rpx; color: #333; border: 1rpx solid #e0e0e0; box-sizing: border-box; }
	.field-input[disabled] { background-color: #f5f5f5; color: #999; border-color: #e8e8e8; }
	.field-picker { width: 100%; height: 70rpx; background-color: #fff; border-radius: 8rpx; padding: 0 20rpx; display: flex; align-items: center; border: 1rpx solid #e0e0e0; box-sizing: border-box; }
	.field-picker[disabled] { background-color: #f5f5f5; color: #999; border-color: #e8e8e8; }
	.field-textarea { width: 100%; min-height: 120rpx; background-color: #fff; border-radius: 8rpx; padding: 15rpx 20rpx; font-size: 26rpx; color: #333; border: 1rpx solid #e0e0e0; box-sizing: border-box; }
	.field-textarea[disabled] { background-color: #f5f5f5; color: #999; border-color: #e8e8e8; }
	.reference-section { background-color: #f8fafc; border-radius: 12rpx; padding: 20rpx; }
	.form-hint { font-size: 24rpx; color: #999; display: block; margin-bottom: 20rpx; }
	.reference-list { margin-bottom: 20rpx; }
	.reference-item { background-color: #fff; border-radius: 10rpx; padding: 20rpx; margin-bottom: 15rpx; border: 1rpx solid #e0e0e0; }
	.reference-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15rpx; }
	.reference-type { font-size: 24rpx; color: #409EFF; font-weight: 500; padding: 4rpx 12rpx; background-color: #ecf5ff; border-radius: 6rpx; }
	.reference-delete { width: 40rpx; height: 40rpx; background-color: #f56c6c; color: #fff; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 28rpx; }
	.reference-image { width: 100%; height: 300rpx; border-radius: 8rpx; }
	.reference-text { font-size: 26rpx; color: #333; line-height: 1.6; display: block; padding: 10rpx 0; }
	.reference-link { font-size: 26rpx; color: #409EFF; display: block; padding: 10rpx 0; text-decoration: underline; }
	.add-reference-btn { display: flex; align-items: center; justify-content: center; padding: 20rpx; background-color: #ecf5ff; border: 2rpx dashed #409EFF; border-radius: 10rpx; color: #409EFF; }
	.add-icon { font-size: 36rpx; margin-right: 10rpx; font-weight: bold; }
	.add-text { font-size: 26rpx; font-weight: 500; }
	.reference-notice { background-color: #fef0f0; border-radius: 8rpx; padding: 15rpx 20rpx; margin-bottom: 30rpx; }
	.reference-notice .notice-text { font-size: 22rpx; color: #f56c6c; line-height: 1.6; }
	.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.5); display: flex; align-items: center; justify-content: center; z-index: 999; }
	.modal-content { width: 85%; max-height: 80%; background-color: #fff; border-radius: 20rpx; padding: 40rpx; display: flex; flex-direction: column; }
	.modal-title { font-size: 34rpx; font-weight: bold; color: #333; display: block; margin-bottom: 30rpx; text-align: center; }
	.modal-body { flex: 1; margin-bottom: 30rpx; max-height: 600rpx; }
	.modal-text { font-size: 26rpx; color: #333; line-height: 1.8; white-space: pre-wrap; }
	.modal-btn { width: 100%; height: 80rpx; background-color: #409EFF; color: #fff; font-size: 30rpx; border-radius: 40rpx; }
	.submit-section { margin-top: 40rpx; padding-top: 30rpx; border-top: 2rpx solid #f0f0f0; }
	.submit-btn { width: 100%; height: 88rpx; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #fff; font-size: 32rpx; font-weight: bold; border-radius: 44rpx; display: flex; align-items: center; justify-content: center; border: none; }
	.submit-btn[disabled] { background: #ccc; color: #fff; }
	.submit-hint { font-size: 24rpx; color: #999; display: block; text-align: center; margin-top: 15rpx; }
	.success-modal { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.6); display: flex; align-items: center; justify-content: center; z-index: 10000; }
	.success-content { background-color: #fff; border-radius: 20rpx; padding: 60rpx 40rpx; width: 80%; max-width: 600rpx; display: flex; flex-direction: column; align-items: center; }
	.success-icon { font-size: 80rpx; color: #67c23a; margin-bottom: 20rpx; }
	.success-title { font-size: 36rpx; font-weight: bold; color: #333; margin-bottom: 15rpx; }
	.success-text { font-size: 28rpx; color: #666; margin-bottom: 40rpx; text-align: center; }
	.success-btn { background-color: #409EFF; color: #fff; font-size: 28rpx; padding: 20rpx 60rpx; border-radius: 40rpx; border: none; }


/* 干员列表区块 */
	.market-char-section {
		margin-top: 30rpx;
		padding: 24rpx;
		background: #f8f9fa;
		border-radius: 16rpx;
	}

/* 市价反馈区块 */
	.character-price-section {
		margin-top: 30rpx;
		padding: 24rpx;
		background: #f8f9fa;
		border-radius: 16rpx;
	}

	.character-price-section .section-title {
		font-size: 28rpx;
		font-weight: 600;
		color: #333;
		margin-bottom: 16rpx;
	}

	.market-price-tip {
		font-size: 24rpx;
		color: #999;
		margin-bottom: 20rpx;
	}

/* 干员选择chips */
	.character-chips {
		display: flex;
		flex-wrap: wrap;
		gap: 16rpx;
	}

	.character-chip {
		padding: 12rpx 24rpx;
		background: #fff;
		border: 2rpx solid #ddd;
		border-radius: 30rpx;
		font-size: 24rpx;
		color: #666;
	}

	.character-chip.selected {
		background: #409EFF;
		border-color: #409EFF;
		color: #fff;
	}

/* 手动新增干员 */
	.custom-char-row {
		display: flex;
		align-items: center;
		gap: 16rpx;
		margin-bottom: 20rpx;
		padding: 16rpx;
		background: #fff;
		border-radius: 12rpx;
		border: 2rpx dashed #dcdfe6;
	}

	.custom-char-input {
		flex: 1;
		height: 64rpx;
		background: #f5f7fa;
		border-radius: 8rpx;
		padding: 0 16rpx;
		font-size: 26rpx;
	}

	.custom-char-add-btn {
		padding: 12rpx 28rpx;
		background: #409EFF;
		border-radius: 30rpx;
		color: #fff;
		font-size: 24rpx;
	}

/* 市价填写区 */
	.price-input-area {
		background: #fff;
		border-radius: 12rpx;
		padding: 20rpx;
	}

	.price-char-item {
		margin-bottom: 24rpx;
		padding-bottom: 24rpx;
		border-bottom: 1rpx solid #eee;
	}

	.price-char-item:last-child {
		margin-bottom: 0;
		padding-bottom: 0;
		border-bottom: none;
	}

	.price-char-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 16rpx;
	}

	.price-char-name {
		font-size: 26rpx;
		font-weight: 600;
		color: #333;
	}

	.price-char-remove {
		font-size: 24rpx;
		color: #f56c6c;
	}

	.price-inputs-row {
		display: flex;
		gap: 20rpx;
	}

	.price-input-group {
		flex: 1;
		display: flex;
		align-items: center;
		gap: 12rpx;
	}

	.price-input-label {
		font-size: 24rpx;
		color: #666;
		min-width: 60rpx;
	}

	.price-input {
		flex: 1;
		height: 64rpx;
		background: #f5f7fa;
		border-radius: 8rpx;
		padding: 0 16rpx;
		font-size: 26rpx;
	}
	
/* 隐私协议弹窗 */
.privacy-modal {
	position: fixed;
	top: 0; left: 0; right: 0; bottom: 0;
	background-color: rgba(0, 0, 0, 0.6);
	display: flex;
	align-items: center;
	justify-content: center;
	z-index: 1000;
}
.privacy-content {
	width: 80%;
	max-width: 600rpx;
	background-color: #fff;
	border-radius: 16rpx;
	overflow: hidden;
}
.privacy-header {
	padding: 30rpx;
	background-color: #409EFF;
	text-align: center;
}
.privacy-title {
	font-size: 32rpx;
	font-weight: bold;
	color: #fff;
}
.privacy-body {
	padding: 40rpx 30rpx;
}
.privacy-text {
	display: block;
	font-size: 28rpx;
	color: #333;
	line-height: 1.6;
	margin-bottom: 20rpx;
}
.privacy-footer {
	display: flex;
	padding: 20rpx 30rpx 40rpx;
	gap: 20rpx;
}
.privacy-btn {
	flex: 1;
	padding: 20rpx 0;
	border-radius: 8rpx;
	text-align: center;
	line-height: 1.5;
	margin: 0;
	border: none;
}
.privacy-btn.cancel {
	background-color: #f0f0f0;
}
.privacy-btn.confirm {
	background-color: #409EFF;
}
.privacy-btn .btn-text {
	font-size: 28rpx;
	color: #666;
}
.privacy-btn.confirm .btn-text {
	color: #fff;
}
.theme-ark .privacy-content { background-color: #2a2a2a; }
.theme-ark .privacy-text { color: #e0e0e0; }
.theme-ark .privacy-btn.cancel { background-color: #444; }
.theme-ark .privacy-btn.cancel .btn-text { color: #ccc; }
</style>
