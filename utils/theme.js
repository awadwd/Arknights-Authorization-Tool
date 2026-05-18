/**
 * 主题配置文件
 * 定义简约风格和科技风格的样式变量
 */

// 主题配置对象
const THEMES = {
	// 简约风格（默认）
	simple: {
		name: '简约风格',
		desc: '清新简洁的界面设计',
		// 颜色配置
		colors: {
			// 背景色
			bg: '#f5f5f5',
			bgGradient: null,
			// 卡片背景
			cardBg: '#ffffff',
			cardBgGradient: null,
			// 主要强调色
			primary: '#409EFF',
			// 成功色
			success: '#67C23A',
			// 警告色
			warning: '#E6A23C',
			// 危险色
			danger: '#F56C6C',
			// 文字颜色
			textPrimary: '#333333',
			textSecondary: '#666666',
			textTertiary: '#999999',
			// 边框颜色
			border: '#f0f0f0',
			// 阴影
			shadow: '0 4rpx 20rpx rgba(0, 0, 0, 0.05)'
		},
		// 圆角配置
		radius: {
			small: '8rpx',
			medium: '12rpx',
			large: '16rpx',
			xlarge: '50rpx'
		},
		// 装饰配置
		decoration: {
			enabled: false,
			corner: false,
			grid: false,
			scanLine: false
		}
	},

	// 科技风格（明日方舟主题）
	ark: {
		name: '科技风格',
		desc: '明日方舟主题深色界面',
		// 颜色配置
		colors: {
			// 背景色
			bg: '#0D0D15',
			bgGradient: 'linear-gradient(180deg, #0D0D15 0%, #1A1A2E 50%, #0D0D15 100%)',
			// 卡片背景
			cardBg: '#16162A',
			cardBgGradient: 'linear-gradient(135deg, #16162A 0%, #1E1E3A 100%)',
			// 主要强调色（方舟橙）
			primary: '#FF6B35',
			// 成功色（科技青）
			success: '#00D4AA',
			// 警告色
			warning: '#FF6B35',
			// 危险色
			danger: '#FF4444',
			// 文字颜色
			textPrimary: '#E8E8F0',
			textSecondary: '#8A8AA0',
			textTertiary: '#6A6A80',
			// 边框颜色
			border: '#2A2A4A',
			// 阴影
			shadow: '0 4rpx 20rpx rgba(0, 0, 0, 0.3)'
		},
		// 圆角配置
		radius: {
			small: '2rpx',
			medium: '4rpx',
			large: '8rpx',
			xlarge: '4rpx'
		},
		// 装饰配置
		decoration: {
			enabled: true,
			corner: true,
			grid: true,
			scanLine: true
		}
	}
};

// 获取主题配置
function getThemeConfig(themeName) {
	return THEMES[themeName] || THEMES.simple;
}

// 获取当前主题（从本地存储读取）
function getCurrentTheme() {
	try {
		const themeMode = uni.getStorageSync('themeMode');
		if (themeMode && (themeMode === 'simple' || themeMode === 'ark')) {
			return themeMode;
		}
	} catch (e) {
		console.error('读取主题设置失败:', e);
	}
	return 'simple';
}

// 保存主题设置
function saveTheme(themeName) {
	try {
		uni.setStorageSync('themeMode', themeName);
		return true;
	} catch (e) {
		console.error('保存主题设置失败:', e);
		return false;
	}
}

// 获取主题类名
function getThemeClass(themeName) {
	return `theme-${themeName || getCurrentTheme()}`;
}

// 获取主题样式对象（用于动态绑定 style）
function getThemeStyles(themeName) {
	const config = getThemeConfig(themeName);
	const colors = config.colors;

	return {
		// 容器背景
		containerBg: colors.bgGradient || colors.bg,
		// 卡片背景
		cardBg: colors.cardBgGradient || colors.cardBg,
		// 文字颜色
		textPrimary: colors.textPrimary,
		textSecondary: colors.textSecondary,
		textTertiary: colors.textTertiary,
		// 边框
		border: colors.border,
		// 阴影
		shadow: colors.shadow,
		// 圆角
		radius: config.radius
	};
}

// 导出模块
module.exports = {
	THEMES,
	getThemeConfig,
	getCurrentTheme,
	saveTheme,
	getThemeClass,
	getThemeStyles
};
