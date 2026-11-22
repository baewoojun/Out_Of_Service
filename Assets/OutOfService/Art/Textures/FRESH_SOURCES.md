# Fresh food and package artwork

Created with the built-in OpenAI image generation tool for this project on 2026-11-10. No paid assets or third-party product photographs were imported in this revision.

- `fresh_food_material_atlas_v1.png`: four photographic albedo swatches, applied with quadrant UV transforms to original 3D cuts and fish meshes.
- `../Packs/ramen_front_atlas_v2.png`: six original package backdrops; `Tools/GenerateMarketPacks.ps1` adds Korean product names and exports the six ramen fronts. These are original prototype designs, not official brand scans.
- Fresh tray, meat, fish, shrimp, door and escalator geometry is authored in the project generator.
- Store fixture reference: [Arneg Korea, Homeplus Gayang](https://www.arneg.co.kr/en/installations/homeplus). Reference only; site photos were not copied into the game.

## Final generation prompts

Fresh atlas:

Use case: photorealistic-natural. Asset type: game material texture atlas for a realistic Korean supermarket meat and seafood counter. Generate ONE square image divided precisely into a seamless 2 by 2 grid of four equally sized square MATERIAL SWATCHES. No borders, no text, no objects or trays, no perspective or cast shadows. Each swatch fills its whole quadrant edge to edge, flat orthographic evenly lit closeup. TOP LEFT: deep red raw beef muscle with fine irregular ivory fat marbling, moist believable tissue. TOP RIGHT: raw pork belly cross section with alternating creamy fat and pink muscle bands, natural irregular structure. BOTTOM LEFT: raw salmon flesh, orange coral tissue with fine pale chevron fat lines. BOTTOM RIGHT: fresh whole mackerel skin surface, silvery small overlapping scales with subtle blue green darker mottling, no fins or eyes. Real photographic detail, neutral white balance, no stylization, no cooking, no blood pools. Intended as albedo textures on separately modeled 3D groceries.

Ramen atlas:

Use case: product-mockup. Asset type: ONE game texture atlas, six original Korean instant ramen package FRONT PRINT DESIGNS in a precise 3 columns by 2 rows grid, each equal square. Not photographs of physical bags, no perspective, no scene, no borders between cells. Full bleed artwork. Each cell has a DIFFERENT photoreal appetizing noodle serving photograph and a different commercial Korean grocery package graphic composition. Cell1 spicy beef red broth noodles, deep red with cream sweeping paper label. Cell2 spicy yellow orange chicken broth noodles, bright yellow with red brush bands. Cell3 thick udon seafood noodles with kelp and mussels, navy blue with red side band. Cell4 mild beef broth noodles with mushrooms, warm orange and ivory with a round central frame. Cell5 black bean jjajang noodles, black and gold with green onion highlights. Cell6 cold spicy bibim noodles with cucumber, white and vivid red with energetic curved stripes. Food occupies middle 60 percent; leave solid contrasting clear panels in upper 15 percent and bottom 25 percent for exact text added later. Natural realistic food photography incorporated into print artwork. NO text, no lettering, no logos, no watermark. Distinct design and distinct food photo in every cell. Professional grocery packaging print.
