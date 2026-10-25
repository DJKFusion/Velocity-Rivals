# Career and garage expansion — 0.6.0

## Earn and spend credits

New and existing profiles begin this economy with 500 credits. Existing XP, cars,
medals, cosmetics and settings remain. The existing free XP car unlocks still work.
The dealership also lets you buy the next car early: Bulwark 500, Phantom 800,
Apex 1,100, Nomad 1,400, Comet 1,700 credits. Cars already unlocked are never charged
for again. A purchased car becomes the selected car; ownership survives reload.

A completed event pays 150 credits, plus 150 for victory, plus up to 100 based on
score. An incomplete timed-out race pays no credits. Training, practice and aborted
events pay no credits. Championship rewards arrive after the final round.

Career medals pay an extra 100 credits per newly improved medal level: first bronze
100, bronze to silver 100, silver to gold 100; going straight to gold pays 300.
Replaying the same medal or earning a lower medal does not repeat the bonus.
Previously earned medals do not receive retroactive bonuses. Results show total,
event and medal credits, wallet balance and a direct garage button.

## Performance workshop

Every owned car has three independent upgrade paths, each with three stages:

| Path | Increase per stage | Stage prices |
| --- | --- | --- |
| Engine | 3% top speed, 5% acceleration | 150 / 300 / 450 credits |
| Handling | 8% grip, 3% steering response | 150 / 300 / 450 credits |
| Boost | 8% boost force | 150 / 300 / 450 credits |

Percentages add relative to base stats; they do not compound. Upgrades apply at
event start and never modify the shared base stat assets. Ice still reduces grip.
Time Attack, including its career events, always uses stock performance so existing
times and ghosts remain comparable. Other records are separated by their upgrade
combination and labeled STOCK or E/H/B levels. Visual paint and body options remain free.

Purchases have a confirmation screen, cost and balance preview. Insufficient funds,
locked cars and maxed upgrades cannot be purchased. A failed save rolls the purchase
back in memory and displays the save error. Event rewards, medal progress and bonuses
are written together in one atomic profile save. Credits cap at 9,999,999.

## Verification

47 standalone C# checks pass, including prices, caps, ownership, reward rules,
normalization and independent rollback copies. Source/asset checks pass. Added Unity
tests cover old-profile defaults, persistent purchases, failed-save rollback, record
separation, dealer/workshop button navigation, stock Time Attack and non-repeating
career bonuses. They are authored but cannot run in this cloud, which has no Unity
Editor. Build-SinglePlayer.cmd runs those tests before packaging on Windows.

Application version remains 0.6.0; no new profile schema version or circuit layout
revision is needed. Before shipping, test the UI on controller and keyboard and
drive the upgraded cars through all twelve events to tune handling and reward pace.
