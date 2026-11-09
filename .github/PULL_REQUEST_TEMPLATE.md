## 变更说明

<!-- 简要说明本次改动的内容与动机 -->

## 关联 Issue

<!-- 例如：Closes #123 -->

## 改动类型

- [ ] Bug 修复
- [ ] 文档改进
- [ ] 重构（不改变外部行为）
- [ ] 新功能（请先确认是否符合项目规划）

## 自检清单

- [ ] 本地已通过 `dotnet build chakra_api_back_end/Chakra.slnx`（零警告）与 `dotnet test`
- [ ] 前端已通过 `npm run lint` 与 `npm run typecheck`
- [ ] 涉及接口变更时已同步更新 Swagger 契约与相关文档
- [ ] 未引入 `Number(id)` 等会截断雪花 ID 的写法
- [ ] 涉及文件操作时已校验路径严格位于目标根目录内
