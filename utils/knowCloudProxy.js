// 自建 API 回落层
// 用法：在 knowCloudRequest 开头先 await fetchSelfHosted(tableName, options)
//        返回非 null 即命中自建接口；返回 null 则调用方继续走原知晓云逻辑。
//
// ⚠️ SELF_HOSTED_BASE 需替换为你的 Cloudflare Pages 实际部署地址
//    （在 Cloudflare 控制台 Pages 项目里查看，形如 https://<project>.pages.dev）
//    并已将该域名加入微信公众平台「request 合法域名」白名单。

export const SELF_HOSTED_BASE = "https://arknightsauthorizationseriesdata-editor.pages.dev";

export async function fetchSelfHosted(tableName, options = {}) {
  try {
    let url = `${SELF_HOSTED_BASE}/api/kc/${encodeURIComponent(tableName)}`;
    if (options && options.data) {
      const params = [];
      Object.keys(options.data).forEach((key) => {
        params.push(`${encodeURIComponent(key)}=${encodeURIComponent(options.data[key])}`);
      });
      if (params.length) url += `?${params.join("&")}`;
    }
    const res = await uni.request({
      url,
      method: "GET",
      timeout: 15000
    });
    if (res.statusCode === 200 && res.data && Array.isArray(res.data.objects)) {
      // 空数组按「未命中」处理：自建 API 对未收录的表名会返回 {"objects":[]}，
      // 若当成命中会阻断向知晓云的回落，表现为「明明有数据却取到空」。
      if (res.data.objects.length === 0) return null;
      return res.data;
    }
    return null;
  } catch (e) {
    return null;
  }
}
