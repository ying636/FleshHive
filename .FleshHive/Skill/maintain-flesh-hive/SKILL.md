---
name: maintain-flesh-hive
description: 理解、维护和扩展 RimWorld Mod《Flesh Hive / 血肉巢穴》。当 Codex 需要分析或修改本 Mod 的血肉巢穴系统、地图级营养与规模、血肉地形扩张、巢群建筑蓝图、血肉喷口、血肉囊消化、寄生系统、扭曲肉资源、meld 分裂/光环/能力、Hive Creature Framework 集成、XML Def 与 C# 架构时使用。
---

# 血肉巢穴 Mod 理解指南

## 使用方式

先把本 Mod 当成一个建立在 `HiveCreatureFramework` 之上的“可控血肉巢群”扩展，而不是普通动物包。核心逻辑由 XML Def 描述内容，由 `.FleshHive/FleshHive` 下的 C# 提供地图状态、寄生、资源搬运、自动建筑和特殊能力。

需要更细的文件路径、类名、Def 文件对应关系时，读取 `references/mod-map.md`。

## 核心概念

- **地图级血肉巢穴状态**：每张地图有一个 `MapComponent_FleshHive`，保存 `MapFleshHive`。它追踪血肉地形规模 `hiveScale`、全局营养 `nutrition`、玩家血肉兽缓存、需要补充扭曲肉的单位、血肉建筑蓝图、血肉喷口和资源搬运体。
- **血肉规模**：由地图上的 `TerrainDefOf.Flesh` 数量决定。`Patch_TerrainGrid_FleshHiveScale` 监听地形变化，`MapComponent_FleshHive.RecalculateHiveScale()` 可重新扫描。营养上限是 `hiveScale * 10`。
- **巢穴营养**：`HiveResource_FleshHiveNutrition` 把 HCF 的 `HiveResource` 映射到地图级 `MapFleshHive.nutrition`，不是每个建筑独立存储。
- **血肉建筑**：玩家通过 HCF 的 `HiveBuildingDef` 放置建筑，但生成的是 `Blueprint_FleshBuild`。蓝图不需要殖民者搬运，会由 `MapComponent_FleshHive` 周期性从巢穴资源或血肉喷口取材料并自动推进施工。
- **寄生系统**：宿主获得 `ParasitismSystem` hediff；被寄生的血肉兽 Pawn 被深度保存到 `ParasitismHediff.flesh` 中。不同血肉兽通过 `ParasitismCompProperties` 定义占用空间、对应 hediff、图标、能力说明和扭曲肉容量。
- **扭曲肉资源**：两套入口共用 `TwistedFleshUtility`：普通单位可有 `CompTwistedFlesh`，寄生宿主则由 `ParasitismSystem` 汇总寄生体容量。能力、力场、meld 分裂等会消耗扭曲肉。
- **血肉地形扩张**：巢穴建筑上的 `CompFleshSpread` 按半径、间隔和权重侵蚀周围可用地形为 `TerrainDefOf.Flesh`，并支持 HCF 进化转移。
- **血肉囊消化**：`FleshSack` 是可容纳 Pawn/Corpse 的建筑。被标记的倒地生物会被搬入血肉囊，血肉囊杀死目标、按体型消化并持续给地图巢穴营养。
- **meld 系统**：meld 类巨型血肉兽拥有更复杂的 comp，例如受伤生成小血肉兽的 `CompMeldSplit`、全图友方血肉兽增益 `CompUnifyAura`、猩红力场 `CompScarletField` 等。

## 主要系统

### 1. 地图巢穴与资源

入口类：`MapComponent_FleshHive`、`MapFleshHive`、`HiveResource_FleshHiveNutrition`、`HiveResourcer`。

功能：
- 维护每张地图的巢穴规模和营养。
- 缓存友方血肉兽，供狂暴、统一光环等全图能力使用。
- 缓存血肉蓝图和血肉喷口，自动调度资源搬运。
- 地图生成时创建一个 HCF `UnitGroup`，标签为 `Flesh`，用于管理玩家血肉兽群组。

关键数据流：
`Flesh terrain 数量 -> hiveScale -> 营养上限 -> HCF HiveResource 显示/消耗 -> 蓝图建造与能力消耗`

### 2. 血肉建筑与自动施工

入口类：`FleshHiveBuildingDef`、`HiveBuildingWorker_FleshBlueprint`、`Blueprint_FleshBuild`、`FleshBlueprintUtility`、`Building_FleshHopper`。

功能：
- HCF 建筑菜单放置 `HiveBuildingDef`，实际生成血肉蓝图。
- 蓝图从 `needResources` 或 `needThings` 中找下一个材料需求。
- `MapComponent_FleshHive` 每 250 tick 尝试生成 `HiveResourcer`，从最近的 `CompHiveResource` 或血肉喷口取材料给蓝图。
- 材料满足后，蓝图每 10 tick 增加 20 工作量，完成后生成目标建筑。
- `PlaceWorker_RequireFleshTerrain` 限制部分建筑必须建在血肉地形上。

### 3. 血肉地形扩张

入口类：`CompFleshSpread`、`CompProperties_FleshSpread`、`Patch_TerrainGrid_FleshHiveScale`。

功能：
- 以建筑为中心维护 `infectedCells`。
- 周期性建立边缘缓存 `borderCache`，按距离和随机权重选择格子侵蚀成血肉地形。
- 初始生成时可执行多次侵蚀，快速铺开巢穴范围。
- 与 HCF 进化系统通过 `ITransfer` 保留扩张状态。

### 4. 寄生系统

入口类：`FleshParasitePod`、`ParasitismSystem`、`ParasitismHediff`、`ParasitismComp`、`Window_SelectParasite`、`FormulaMaterial_Parasite`。

功能：
- 血肉寄生仓把目标宿主和血肉兽组合，完成后给宿主添加 `ParasitismSystem` 与具体 `ParasitismHediff`。
- `ParasitismSystem.Limit` 来自 `FH_Stat_ParasitismCapacity`，`Count` 来自所有寄生 hediff 的空间占用。
- 寄生体 Pawn 不销毁，而是保存到 `ParasitismHediff.flesh`；移除寄生时可重新生成出来。
- 寄生体可以给宿主提供能力、触手渲染、免疫、再生、统一光环、扭曲肉容量等。
- `Patch_PawnComponents_Parasitism` 确保被寄生的 Pawn 即使原本没有 ability tracker 也能使用寄生能力。

### 5. 扭曲肉与补充

入口类：`CompTwistedFlesh`、`ParasitismSystem`、`TwistedFleshUtility`、`WorkGiver_RefillTwistedFlesh`、`JobDriver_RefillTwistedFlesh`。

功能：
- 扭曲肉是单位级能量池。
- 普通单位由 `CompTwistedFlesh` 保存容量和当前值。
- 寄生宿主由 `ParasitismSystem` 汇总所有寄生体的 `twistedFleshCapacity`。
- 能力和防护效果通过 `TwistedFleshUtility.CanConsumeTwistedFlesh/ConsumeTwistedFlesh` 统一消费。
- 地图组件缓存需要补充扭曲肉的 Pawn，WorkGiver/Job 负责补充。

### 6. 血肉囊与猎物标记

入口类：`Designator_MarkPrey`、`WorkGiver_FillFleshSack`、`JobDriver_FillFleshSack`、`FleshSack`。

功能：
- 玩家用“标记猎物” designator 标记倒地血肉生物。
- WorkGiver 找到被标记目标和可用血肉囊，生成搬运 Job。
- 血肉囊接收 Pawn 后立即击杀，按 `BodySize * 5 天` 设置消化时间。
- 消化期间每 tick 增加巢穴营养，完成后吐出尸体/残留。

### 7. Meld 与战斗能力

入口类：`CompMeldSplit`、`CompUnifyAura`、`CompScarletField`、`CompAbilityEffect_FH_FleshBerserk`、`CompAbilityEffect_FH_FleshSpread`、`CompAbilityEffect_FH_ScarletField`。

功能：
- `CompMeldSplit`：meld 第一次受伤会生成若干小血肉兽；累计伤害跨过阈值时，消耗 100 扭曲肉并按战斗力预算继续生成小血肉兽。
- `CompUnifyAura`：周期性给同阵营、同地图的血肉兽添加 `FH_Unification`，离开范围/失效时移除。
- `CompScarletField`：激活后绘制血肉护盾；吸收近战/远程伤害，拦截范围内投射物，并消耗扭曲肉。
- `Flesh Berserk`：给地图上友方血肉兽添加 `FH_Berserk`。
- `Flesh Spread`：消耗扭曲肉，把目标区域转成血肉地形并造成爆炸伤害。
- 消耗扭曲肉的能力统一在 AbilityDef 的 `<comps>` 最前面挂 `FleshHive.CompProperties_AbilityEffect_TwistedFleshCost`，由它负责 `CanCast`、`GizmoDisabled` 和实际扣除扭曲肉；具体效果 comp 不要再自行调用 `TwistedFleshUtility.ConsumeTwistedFlesh`，避免双扣或按钮未禁用。
- 本 mod 内随机生成/选择血肉兽统一走 `FleshHiveFleshbeastSpawnUtility`；死亡动作、受伤分裂、裂殖母兽技能、喷吐/释放类效果不要各自手写 `PawnGenerationRequest` 或重复随机池逻辑。

## XML 与 C# 的关系

- `1.6/Defs/ThingDef/Race.xml` 定义普通血肉兽、PawnKind、身体部件、死亡行为和寄生 comp。
- `1.6/Defs/ThingDef/Race_Melds.xml` 定义 meld 血肉兽，并挂载分裂、光环、力场、扭曲肉等 comp。
- `1.6/Defs/ThingDef/Buildings_Natural.xml` 定义自然/实体建筑，例如血肉巢穴、主巢、寄生仓、血肉囊、墙块、喷口、家具。
- `1.6/Defs/HiveBuildingDefs/Buildings.xml` 定义 HCF 建筑蓝图入口，通常对应 `Buildings_Natural.xml` 的实际 `ThingDef`。
- `1.6/Defs/HediffDefs/*.xml` 定义寄生、扭曲肉、统一、狂暴、免疫等状态。
- `1.6/Defs/AbilityDefs/*.xml` 定义能力，C# comp 决定具体效果。
- `1.6/Defs/UnitCategoryDefs/*.xml` 定义 HCF 生成分类和单位配方。
- `Patches/Patches.xml` 修改外部 fleshbeast/meld 行为，并把 `FleshHive.Designator_MarkPrey` 加到 Orders。

## 修改前判断

如果要改的是玩法系统，先定位它属于哪条链：

- 巢穴营养/规模：`MapComponent_FleshHive`、`MapFleshHive`、`HiveResource_FleshHiveNutrition`、血肉地形 patch。
- 建筑/施工：`HiveBuildingDefs`、`Buildings_Natural.xml`、`Blueprint_FleshBuild`、`Building_FleshHopper`。
- 寄生：`FleshParasitePod`、`ParasitismSystem`、`ParasitismHediff`、相关 hediff/comp Def。
- 单位能量：`CompTwistedFlesh`、`ParasitismSystem`、`TwistedFleshUtility`。
- 血肉囊：`Designator_MarkPrey`、Job/WorkGiver、`FleshSack`。
- meld 能力：`Race_Melds.xml`、`CompMeldSplit`、`CompUnifyAura`、`CompScarletField`、AbilityDefs。

涉及 `defName`、Scribe 字段、保存的 Pawn、MapComponent、HediffWithComps、ThingComp 持久字段、HCF 接口契约时，先说明存档/兼容性风险并等待确认。
