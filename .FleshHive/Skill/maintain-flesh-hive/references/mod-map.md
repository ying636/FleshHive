# 血肉巢穴架构索引

## 总览

《血肉巢穴》是依赖 `HiveCreatureFramework` 的 RimWorld 1.6 Mod。它把血肉兽从敌对生物扩展为玩家可管理的巢群体系：地图上有血肉规模和营养，玩家通过巢群建筑菜单建造血肉器官建筑，消化生物获得营养，使用寄生体强化宿主，靠扭曲肉驱动特殊能力，并通过 meld 巨兽提供高级战斗/支援能力。

## 依赖与发布结构

- `About/About.xml`：`packageId` 为 `HaiLuan.FleshHive`，依赖 `HaiLuan.HiveCreatureFramework`。
- `LoadFolders.xml`：RimWorld `1.6` 加载根目录和 `1.6`。
- `.FleshHive/FleshHive.csproj`：源码项目，目标 `net48`，引用 `Krafs.Rimworld.Ref`、`Lib.Harmony.Ref` 和本地 `HiveCreatureFramework.dll`。
- `1.6/Assemblies/net48/FleshHive.dll`：发布程序集。
- `PatchMain`：静态构造中 `Harmony("FH_Patch").PatchAll()`，所有 Harmony patch 自动注册。

## 系统到文件的映射

| 系统 | 主要 C# | 主要 XML |
| --- | --- | --- |
| 地图巢穴状态 | `Map/MapComponent_FleshHive.cs`, `Map/MapFleshHive.cs` | `HiveResourceDefs/Resources.xml`, `ThingDef/Buildings_Natural.xml` |
| 血肉地形规模 | `Map/Patch_TerrainGrid_FleshHiveScale.cs`, `Hive/CompFleshSpread.cs` | `ThingDef/Buildings_Natural.xml`, `HiveEvolutionDefs/FleshHiveEvolution.xml` |
| 巢群建筑蓝图 | `Building/Blueprint_FleshBuild.cs`, `Building/HiveBuildingWorker_FleshBlueprint.cs`, `Building/FleshBlueprintUtility.cs` | `HiveBuildingDefs/Buildings.xml`, `ThingDef/Buildings_Natural.xml` |
| 血肉喷口与产物 | `Building/Building_FleshHopper.cs`, `Core/FleshHopperUtility.cs`, `Spawner/*` | `ThingDef/Buildings_Natural.xml`, HCF formula/spawn data |
| 血肉囊消化 | `FleshSack/FleshSack.cs`, `Designator_MarkPrey.cs`, `JobDriver_FillFleshSack.cs`, `WorkGiver_FillFleshSack.cs` | `ThingDef/Buildings_Natural.xml`, `DesignationDefs/Designations.xml`, `JobDefs/Jobs.xml` |
| 寄生系统 | `Parasitism/FleshParasitePod.cs`, `ParasitismSystem.cs`, `ParasitismHediff.cs`, `ParasitismComp/*` | `HediffDefs/Hediffs.xml`, `HediffDefs/Hediffs_Melds.xml`, `ThingDef/Race*.xml`, `Stats/Stats_Misc.xml` |
| 扭曲肉 | `TwistedFlesh/CompTwistedFlesh.cs`, `TwistedFleshUtility.cs`, refill jobs/workgivers | `HediffDefs`, `ThingDef/Race_Melds.xml`, `JobDefs` |
| meld 分裂与光环 | `Comp/CompMeldSplit.cs`, `Comp/CompUnifyAura.cs`, `Comp/CompScarletField.cs` | `ThingDef/Race_Melds.xml`, `HediffDefs/Hediffs_Melds.xml`, `AbilityDefs/Abilities_Melds.xml` |
| 能力与投射物 | `Ability/*`, `Projectile/*`, parasitism effect classes | `AbilityDefs/Abilities.xml`, `AbilityDefs/Abilities_Melds.xml`, `PawnRenderTreeDefs` |
| 研究与进化 | building visibility workers, HCF transfer comps | `ResearchProjectDefs`, `ResearchTabDefs`, `HiveEvolutionDefs` |

## 地图巢穴状态

`MapComponent_FleshHive` 是本 Mod 的地图级中枢。

保存内容：
- `MapFleshHive mapFleshHive`：深度保存 `hiveScale` 和 `nutrition`。
- `UnitGroup group`：HCF 的玩家血肉兽群组，地图生成时创建。
- `cachedFleshBeasts`：玩家/地图血肉兽缓存，供全图能力和光环使用。
- `cachedNeedsTwistedFlesh`：需要补充扭曲肉的 Pawn 缓存。
- `hiveResourcers`：正在飞行/搬运的巢穴资源搬运体。
- `CachedFleshBlueprints`、`CachedFleshHoppers`：存在 `MapFleshHive` 中，运行时懒加载。

关键行为：
- `MapGenerated()` 创建 `TemporaryFleshGroup`，标签 `Flesh`，单位上限 240，默认攻击模式。
- `RecalculateHiveScale()` 扫描整张地图的 `TerrainDefOf.Flesh`。
- `Notify_FleshTerrainChanged(delta)` 由地形 patch 调用，改变血肉规模并裁剪营养上限。
- `MapComponentTick()` 更新搬运体，每 250 tick 尝试给待建血肉蓝图派送资源。

## 营养系统

营养不是某个建筑的私有资源，而是地图级资源：

`MapFleshHive.nutrition` ← 被 `HiveResource_FleshHiveNutrition` 暴露成 HCF `HiveResource`。

上限公式：

`NutritionLimit = hiveScale * 10`

营养来源：
- 血肉囊消化 Pawn，每 tick 增加营养。
- 任何调用 `MapComponent_FleshHive.AddNutrition` 或 `HiveResource_FleshHiveNutrition.IncreaseResource` 的逻辑。

营养消耗：
- HCF 建筑蓝图或公式通过 `CompHiveResource` 找到 `FH_Resource_Nutrition` 后减少。
- `HiveResourcer` 从有资源的巢穴建筑取资源送到蓝图。

## 血肉建筑施工

玩家看到的是 HCF 的 `HiveBuildingDef`，但本 Mod 使用自定义 worker：

1. `HiveBuildingWorker_FleshBlueprint.Place()` 检查位置。
2. `FleshBlueprintUtility.MakeBlueprint(def)` 创建 `Blueprint_FleshBuild`。
3. 蓝图注册到 `MapComponent_FleshHive`。
4. 地图组件寻找最近的 `CompHiveResource` 或 `Building_FleshHopper`。
5. 创建 `HiveResourcer` 搬运资源/物品。
6. `Blueprint_FleshBuild.ReceiveResource/ReceiveThing` 扣除需求。
7. 材料满足后自动增加工作量并 `Complete()`。

注意：
- `Blueprint_FleshBuild.CanBeBuilt => false`，它不走普通殖民者建造逻辑。
- 蓝图会绘制 `_Outline` 贴图，所以新增建筑最好配套 `texPath + "_Outline"`。
- `HiveBuildingWorker_FleshBlueprintResearchVisibility` 和 `HiveBuildingWorker_ResearchVisibility` 控制研究后可见/替代建筑。

## 血肉地形扩张

`CompFleshSpread` 挂在巢穴/主巢等建筑上：

- 初始把建筑占地加入 `infectedCells`。
- `initialErosionCount` 控制生成时立即扩张次数。
- 每 `intervalDays * 60000` tick 执行一次侵蚀。
- `borderCache` 分批构建，每 tick 处理少量格子，避免一次性扫描过重。
- 候选格子必须在半径内、未迷雾、非已有血肉地形。
- 权重离中心越近越高，并加随机系数。
- 设置地形时会触发地图规模更新。

它实现 `ITransfer`，用于 HCF 进化时把已感染格子和倒计时转移到新建筑。

## 寄生系统

寄生是“宿主 Hediff 保存被寄生 Pawn”的结构。

核心对象：
- `FleshParasitePod`：寄生仓建筑，负责选择宿主/血肉兽、进度、UI 和开始寄生。
- `ParasitismSystem`：宿主身上的系统 hediff，提供容量、扭曲肉池、能力 gizmo、寄生体列表。
- `ParasitismHediff`：每个具体寄生体对应一个 hediff，深度保存 `Pawn flesh`。
- `ParasitismComp`：挂在可寄生血肉兽上的 ThingComp，定义 cost、icon、hediff、effect、ability 文本和扭曲肉容量。

流程：
1. 血肉兽进入寄生仓或 HCF 公式选择寄生体。
2. 宿主若没有 `FH_ParasitismSystem`，添加系统 hediff。
3. `ParasitismSystem.Parasite(flesh)` 检查容量。
4. 若血肉兽已生成，先 DeSpawn。
5. 给宿主添加血肉兽对应的 `ParasitismHediff`。
6. 把血肉兽 Pawn 赋值给 `ParasitismHediff.flesh`。
7. 刷新缓存、重新分配触手角度、确保 ability tracker 存在。

容量：
- `Limit` = 宿主 `FH_Stat_ParasitismCapacity`。
- `Count` = 所有 `ParasitismHediff.Count` 之和。
- 当前实现中 `ParasitismHediff.spaceCost` 默认 1；注意与 `ParasitismCompProperties.cost` 的同步风险。

移除：
- `RemoveFlesh` 会把保存的 Pawn 重新生成到寄生仓位置并移除 hediff。
- `ParasitismHediff.PreRemoved` 也会在条件满足时尝试把未生成的 flesh Pawn 放回宿主位置。

## 寄生能力与渲染

寄生体能力来自两层：

- XML/C# hediff comp：例如免疫、触手、扭曲肉生产、统一光环。
- `ParasitismAbilityGizmo`：读取寄生 hediff/comp，提供能力开关或能力入口。

渲染相关类：
- `DynamicPawnRenderNodeSetup_Parasitism`
- `PawnNodeRenderWorker_Tentacle`
- `PawnNodeRenderWorker_ScarletShield`
- `TentacleProperties` 及其子类

寄生触手通过 `AssignAngle()` 给每个 `HediffComp_Parasitism` 分配角度，避免多个触手重叠。

## 扭曲肉系统

扭曲肉是单位可消耗能量：

- 普通 Pawn：`CompTwistedFlesh.currentTwistedFlesh` / `Props.capacity`。
- 寄生宿主：`ParasitismSystem.currentTwistedFlesh` / 所有寄生体 `twistedFleshCapacity` 总和。

统一入口应使用 `TwistedFleshUtility`，不要在新功能里只检查一种实现。

使用者：
- `CompAbilityEffect_FH_FleshSpread` 消耗扭曲肉铺地并爆炸。
- `CompScarletField` / `HediffComp_ScarletField` 消耗扭曲肉吸收伤害、拦截投射物。
- `CompMeldSplit` 在伤害阈值触发时消耗 100 扭曲肉生成小血肉兽。
- refill WorkGiver/Job 给需要补充的 Pawn 补充扭曲肉。

## 血肉囊消化链

`Designator_MarkPrey`：
- 加到 Orders 分类。
- 只能标记倒地目标。
- 目前会检查目标是血肉生物。

`WorkGiver_FillFleshSack`：
- 找可用血肉囊和被标记目标。
- 生成 `FH_Job_FillFleshSack`。

`FleshSack`：
- `ThingOwner<Thing> contents` 保存内部目标。
- `InsertPawn` 把 Pawn 放入容器，设置消化总时间，并用 Crush 伤害击杀。
- 每 tick 增加 `20 / 60000` 营养。
- 消化完成后 `TryDropAll` 吐出内容，尸体 `timeOfDeath = 0`。

## Meld 高级机制

`CompMeldSplit`：
- 第一次受伤：按 `firstHitSpawnCountRange` 生成小血肉兽。
- 累计伤害每跨过 `damageThreshold`：消耗 100 扭曲肉，根据 `thresholdSpawnPointsRange` 的战斗力预算生成小血肉兽。
- 生成后用 `PawnFlyer_Stun` 抛到附近格子，并加入 HCF group。

`CompUnifyAura`：
- 每 60 tick 刷新。
- 对地图组件缓存中的同阵营血肉兽添加 `FH_Unification`。
- 不再满足条件时移除。

`CompScarletField`：
- 由能力切换开关。
- 绘制小护盾和范围护盾。
- 受到伤害前按远程 1、近战 20 扭曲肉吸收。
- 每 2 tick 扫描范围内投射物，消耗 1 扭曲肉并反射调用 `Projectile.Impact(null, true)`。

## 主要 Def 文件说明

- `ThingDef/Race.xml`：普通血肉兽。常见 Def 成对出现：`ThingDef` race 和对应 `PawnKindDef`。
- `ThingDef/Race_Melds.xml`：meld 巨兽和它们的特殊 comp。
- `ThingDef/Buildings_Natural.xml`：实际建筑 ThingDef，包括巢穴、主巢、寄生仓、血肉囊、墙、门、家具、喷口。
- `HiveBuildingDefs/Buildings.xml`：HCF 建筑菜单/蓝图入口。
- `HediffDefs/Hediffs.xml`：普通寄生 hediff、寄生系统、触手等。
- `HediffDefs/Hediffs_Melds.xml`：meld 寄生、统一、狂暴等。
- `AbilityDefs/Abilities.xml`：普通血肉兽/寄生能力和投射物。
- `AbilityDefs/Abilities_Melds.xml`：meld 能力，如狂暴、扩张、猩红力场、冲锋。
- `UnitCategoryDefs/Categories*.xml`：HCF 单位分类和生成配方。
- `FusionDef/Fusions.xml`：HCF 融合配方。
- `HiveEvolutionDefs/FleshHiveEvolution.xml`：巢穴进化。
- `ResearchProjectDefs/FH_ResearchProjects.xml`：研究解锁。
- `Patches/Patches.xml`：对外部 Def 的 patch，尤其 Dreadmeld/Trispike/Bulbfreak 分裂和 Orders designator。

## 修改时的判断规则

- 改营养/规模，先看地图组件和 HCF resource，不要只改建筑 Def。
- 改建筑，必须同时看 `HiveBuildingDef` 和实际 `ThingDef`。
- 改寄生，必须同时看宿主系统 hediff、具体寄生 hediff、血肉兽 `ParasitismComp` 和翻译。
- 改扭曲肉，必须同时支持 `CompTwistedFlesh` 和 `ParasitismSystem` 两套持有者。
- 改 meld，检查 `Race_Melds.xml` 中的 comp 参数和 C# comp 是否都支持。
- 改显示文本，DLL 文本用 Keyed，中英文都要有；Def 文本优先 DefInjected。
- 改 `defName`、Scribe 字段、保存的 Pawn、MapComponent、Hediff/Comp 类名时，先说明存档兼容风险。
