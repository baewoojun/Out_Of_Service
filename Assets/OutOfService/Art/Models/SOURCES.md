# Realistic 3D props

The FBX files and 1K textures in this folder came from [Poly Haven](https://polyhaven.com/license), which licenses its models as CC0. `Tools/DownloadPolyHavenModels.ps1` downloaded them from the official API and verified the published MD5 checksums.

- [Food Apple 01](https://polyhaven.com/a/food_apple_01) — produce island
- [Pears Asian 01](https://polyhaven.com/a/food_pears_asian_01) — Korean pear display
- [Yellow Onion](https://polyhaven.com/a/yellow_onion) — onion produce crates
- [Bananas](https://polyhaven.com/a/bananas) — fruit wall display
- [Lemon](https://polyhaven.com/a/lemon) — citrus display
- [Sweet Potato](https://polyhaven.com/a/sweet_potato) — root vegetable display
- [Steel Frame Shelves 02](https://polyhaven.com/a/steel_frame_shelves_02) — stockroom
- [Multi Cleaner Bottle](https://polyhaven.com/a/multi_cleaner_bottle) — household products
- [Cardboard Box 01](https://polyhaven.com/a/cardboard_box_01) — stockroom

These are reused as background props, not unique hero objects. The packaged Korean shelf stock remains original project artwork.

## Original clothing and hand geometry (2026-11-10)
MarketClothingV7.cs creates original draped garment meshes and metal hangers; MarketHandsV7.cs creates the articulated static first-person hand. No external mesh or paid asset used. Generated geometry is reproducible through Build Market Prototype V7.

MarketShoesV7.cs adds original trainer geometry (separate sole, toe, heel, ankle opening and laces), shoe boxes and fitting benches. No external source asset.

## Original three-floor extension (2026-11-18)

`Editor/MarketThreeFloorsV7.cs` authors the B1 mall units, stair flights/guards/landings, hinged fire doors, appliance displays, family room, customer-service counter and wire shopping carts. `Editor/MarketCirculationV7.cs` authors the open customer circulation paths and atrium glass guards. Author: this project's procedural modelling code. Source/original files: those scripts; reproducible through Build Market Prototype V7. No purchased model, external mesh or copied branding was added. Surface maps reuse the project's documented materials; their existing source terms remain applicable.

The user-provided store-plan photograph, received 2026-11-18, is a reference for perimeter departments, central merchandise blocks, connected main aisles and entrance-side checkouts. No public URL was supplied. The image establishes only the depicted arrangement; it does not establish real dimensions, exact fixture locations, stock or this project's floor allocation. The 100 x 90 m envelope, -5/0/5 m storey heights and B1/1F/2F allocation are game design decisions. The photo itself is not imported as a game texture. Floor names remain B1 mall/convenience, 1F food/home/appliances, 2F fashion/customer service, as confirmed by the user.
