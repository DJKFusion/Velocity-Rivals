# Cosmetic store for direct downloads / itch.io

The garage now contains a Cosmetic Store. Aurora Paint Collection adds jade, rose,
pearl and violet paints for every car. Existing paints stay free, and paid paints
never change handling, boost, credits or progression.

## What is implemented

- Controller-navigable store, explicit browser handoff and installed-pack equip buttons.
- itch.io-hosted checkout: no card form, payment secret or card data in Unity.
- Separate downloadable pack, manual installation, reload and persistent paint selection.
- Showroom, career, training and free-race paint integration.
- Missing/invalid pack falls back to the included paint; saves and progress remain intact.
- Checkout disabled until a valid HTTPS creator.itch.io/product URL is configured.

This is a **DRM-free downloadable DLC flow**, not automatic in-app fulfilment or
verified account ownership. Its small JSON file enables built-in colours; anyone
who copies or recreates that file can enable them. Do not describe this as secure
entitlement verification. Refunds do not automatically remove installed content.
For protected purchases, a separately hosted account/entitlement service and a
payment-provider integration would be additional work.

## Seller setup (required before charging players)

1. Set up your itch.io seller account, payment/payout settings and required tax details.
2. Create a separate paid downloadable product for Aurora Paint Collection. Describe
   the four colours accurately and state that the compatible base game is required.
   Use a fixed price suitable for your market; final checkout price belongs on itch.io.
3. Run `python Tools/package_cosmetics.py`. Upload only
   `Builds/Store/VelocityRivals-Aurora-Paints.zip` as the pack download.
4. Publish the product and copy its HTTPS product URL into `CheckoutUrl` in
   `Assets/Scripts/Gameplay/CosmeticCatalog.cs`. No API keys are needed for this flow.
5. Rebuild the base game with `Build-SinglePlayer.cmd`. Do not bundle the pack JSON
   with the base game. Source archives contain developer tools and are not store releases.
6. Test the real checkout/download/install flow using itch.io's supported testing
   facilities or an authorized purchase before advertising the feature.

No seller account, published product, price, live payment or payout was configured
by this code change. Opening a browser and returning to the game never grants a pack.

## Player installation

Garage > Cosmetic Store > Open Pack Folder. Copy `aurora-paints.json` from the
purchased ZIP into that folder, then select Reload Installed Packs and Equip.
The folder is beside the game's profile in `CosmeticPacks` (normally under
Windows LocalLow/DJKFusion/VelocityRivals). Downloads can be retrieved again from
the buyer's itch.io purchase page. Selecting a regular Garage paint clears the
optional paint selection. Uninstalling a pack restores the included paint.

## Verification before release

Cloud checks cover syntax, catalog/URL checks and pack archive integrity. Unity
EditMode tests cover missing, invalid, installed and removed manifests. Run the
Unity build/test gates on Windows and play-test equip, restart, every mode,
controller navigation and the showroom. Check new and existing profiles. Test
checkout cancellation, actual download, reinstall and refund support separately.
A successful source check does not establish that real payments work.
