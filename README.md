# Mayor of Medieval

Mayor of Medieval is a compact village-production tycoon built in Unity. The player gathers and carries resources, builds a working settlement, automates production with workers, fulfills orders, and grows the village through increasingly complex systems.

> Development status: private playable prototype. WebGL and YouTube Playables builds exist, but the project is not yet prepared for a public source release.

## Core gameplay loop

1. Harvest wood, stone, grain, and water.
2. Carry resources to stockpiles and production buildings.
3. Convert raw materials into products such as bread, swords, and beer.
4. Serve customers, complete quests and royal orders, and earn gold.
5. Unlock buildings, hire workers, train soldiers, and raise village prestige.

## Implemented systems

- Physical harvest, carry, stockpile, and sales flow
- Customer queues, patience, shelf service, and hand delivery
- Multi-resource production chains for the mill, blacksmith, and inn
- Automated fields, wells, workers, and escalating hire costs
- Building pads, upgrade visuals, and guided progression
- Barracks, soldier training, and combat-reward loop
- Daily quests, royal orders, prestige, and permanent mayor decisions
- Local save and autosave systems
- Responsive HUD and virtual joystick support
- WebGL host bridge for save, score/prestige, pause, audio, and language callbacks

## Technology

- Unity `6000.3.11f1`
- C#
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.19.0`
- WebGL / YouTube Playables template and JavaScript bridge
- Git LFS for large assets

## Project map

- `Assets/Scenes/SampleScene.unity` - main gameplay scene
- `Assets/Scripts/Core` - progression, quests, saves, prestige, and platform integration
- `Assets/Scripts/Building` - production, stock, service, and building behavior
- `Assets/Scripts/NPC` - customers, workers, and soldiers
- `Assets/Scripts/Environment` - harvest nodes and carrier helpers
- `Assets/WebGLTemplates/YouTubePlayables` - playable host template
- `Builds/WebGL` and `Builds/YouTubePlayables` - local build outputs

## Development priorities

- Balance progression, economy, and session length
- Verify save and lifecycle behavior across playable hosts
- Improve onboarding, responsive UI, and accessibility
- Audit third-party asset licenses and add root `LICENSE` / `CREDITS` files
- Add gameplay screenshots and a short trailer during the public-release pass

## Ownership and licensing

Created by **Kemal Yılmaz / Poppanda Interactive**.

No open-source license has been granted yet. Until a root `LICENSE` file is added, the project source and original assets are all rights reserved. Third-party assets remain subject to their respective licenses.
