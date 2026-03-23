Battle Rework v2 Script Pack

Included changes:
- Basic attack is handled as SkillDefinition slot 0.
- No global skill pool in BattleManager.
- Party data is treated as prebuilt external battle-entry data.
- Shared party inventory on PartyDefinition.
- Ally info panel uses separated value fields and supports current/original level.
- Enemy basic info panel has no portrait. Enemy detail panel has portrait.
- Single Move button + target selection swap flow.
- TurnMark / TargetMark support in BattleUnitView.
- Target hover preview for enemy-target skills:
  - attack skills: hit chance + damage range + status success
  - success-only enemy skills: success chance + status success
  - items / ally buffs: target marks only
- Existing graze/miss/crit logic kept and expanded with DMG ±10% damage roll.
- Front-slot compression after death.

Important:
- Inspector rebind is required.
- This pack is intended to replace the previous draft scripts.
- Unity compile was not executed in this environment; after import, please resolve any scene-specific binding issues.
