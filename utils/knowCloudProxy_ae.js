// 自建 API 回落层 - package_ArknightEnds 专用
// ArknightEnds 数据在 ArknightsAuthorization_Series_End 仓库
// 目前自建 CF Pages 服务的是主包 ArknightsAuthorization_Series 数据，不用于 ArknightEnds
// 直接返回 null，知晓云请求失败时由调用方自行处理

export async function fetchSelfHosted(tableName, options = {}) {
  return null;
}
