// 自建 API 回落层 - package_ArknightEnds 专用
// package_ArknightEnds 暂时不走自建 CF Pages（该 endpoint 服务的是主包 ArknightsAuthorization_Series 数据）
// 若知晓云 API 请求失败，直接抛错由调用方处理，不走自建回落。

export async function fetchSelfHosted(tableName, options = {}) {
  // package_ArknightEnds 的数据在 ArknightsAuthorization_Series_End 仓库
  // 自建 CF Pages endpoint 目前服务的是主包数据，暂不用于 ArknightEnds
  // 如后续为 ArknightEnds 搭建独立 CF Pages，可在此添加回落逻辑
  return null;
}
