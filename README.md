# Carrom 3D Multiplayer - WordPress Integration

## Features
- 3D Carrom game with Photon multiplayer
- WordPress wallet integration (Rs 10 entry, Rs 18 prize)
- 15-second bot fallback with realistic Indian names
- AdMob ads support

## PHP API Files
Upload the files in `PHP-API/` folder to your server at `/games/carrom/api/`:
- `deduct_fee.php` - Deducts Rs 10 entry fee
- `credit_win.php` - Credits Rs 18 to winner

## Unity Scripts Modified
- `WordPressAPI.cs` - WordPress API connector
- `MenuController.cs` - Entry fee deduction + 15s bot fallback
- `GameController.cs` - Win prize credit
- `Constants.cs` - Rs 10 room prices

## Build Instructions
1. Open in Unity 2023.2.10f1
2. File > Build Settings > WebGL
3. Build

## Server Setup
1. Upload WebGL build to `/games/carrom/`
2. Upload PHP files to `/games/carrom/api/`
3. Ensure user is logged in to WordPress
