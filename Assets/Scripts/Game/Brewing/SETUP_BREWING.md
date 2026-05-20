# Hệ thống pha chế trà sữa / boba — Hướng dẫn setup

Toàn bộ logic gameplay đã được code trong `Assets/Scripts/Game/Brewing/`. Phần code **không
tự dựng được scene** — bạn cần kéo thả GameObject + gán reference trong Unity Editor theo hướng
dẫn dưới đây. Code bám đúng kiến trúc hiện tại (không namespace, MonoBehaviour + `[SerializeField]`,
DI thủ công qua `GameBootstrapper`).

> Yêu cầu: Unity 2D URP (đã có), DOTween (đã có DLL), Physics2D + ParticleSystem (đã có module).

---

## 1. Cấu trúc file đã tạo

```
Game/Brewing/
├── BrewingPhase.cs            # enum luồng: SelectCup → Pour → Topping → Stir → Drink → Done
├── BrewingManager.cs          # điều phối phase, bật/tắt tương tác, cộng điểm khi uống xong
├── Util/DragInput2D.cs        # helper input chuột/touch + khóa kéo 1-ngón dùng chung
├── Cup/CupController.cs        # hub của ly: sprite, mask, liquid, container topping
├── Cup/CupSelector.cs         # đổi sprite ly (CupDefinition list)
├── Liquid/LiquidController.cs # fake liquid: scale theo fill, trộn màu, sprite mask
├── Liquid/LiquidWobble.cs     # sóng nước sine + spring sloshing (không cần shader)
├── Liquid/LiquidWobble.shader # shader ripple/gradient (TÙY CHỌN, chỉ để polish)
├── Pour/IngredientBottle.cs   # chai kéo-thả để rót
├── Pour/PourStream.cs         # dòng nước + splash + bubble particle
├── Toppings/DraggableTopping.cs  # topping kéo-thả vào ly
├── Toppings/ToppingBuoyancy.cs   # vật lý nổi/chìm nhẹ trong nước
├── Toppings/ToppingSpawner.cs    # khay topping tự refill
└── Interaction/StirController.cs # vuốt khuấy/lắc
    Interaction/DrinkController.cs # kéo ly lên để uống, nước rút dần
```

## 2. Map sprite có sẵn → vai trò

| Folder sprite | Dùng cho |
|---|---|
| `Asset/Cup/Asset 1..9` | Sprite thân ly (gán vào `CupDefinition.cupSprite`) |
| `Asset/Jelly/Asset 20..32` | Trân châu / jelly (topping prefab) |
| `Asset/Fruit/Asset 1..12` | Trái cây (topping prefab) |
| `Asset/Ice/Ice*.png` | Đá viên (topping prefab, buoyancy nổi cao) |
| `Asset/Straw/Asset 12..19` | Ống hút (trang trí, đặt cạnh ly khi uống) |
| `Asset/Sticker/*` | Trang trí ly / nhãn |
| `Art/`, `UXUI/` | UI: nút, panel, bảng chọn |

> **Sprite chất lỏng & mặt nước** chưa có sẵn — tạo 2 sprite trắng đơn giản trong Editor:
> một hình **chữ nhật bo góc** (thân nước) và một **ellipse mỏng** (mặt nước/meniscus). Để trắng,
> code sẽ tô màu theo nguyên liệu. Hoặc dùng sprite trắng built-in của Unity rồi scale.

---

## 3. Phân tầng (Sorting Layers / Order in Layer)

Tạo Sorting Layers theo thứ tự (Project Settings → Tags and Layers):
`Background < CupBack < Liquid < Topping < CupFront < Stream < UI`

- **Mask hoạt động theo stencil, không theo sorting** — nhưng order quyết định cái gì đè lên cái gì.
- Nếu ly có cả thành trước & sau: tách 2 SpriteRenderer (CupBack ở sau nước, CupFront ở trước nước)
  để nước nằm "trong" ly. Nếu chỉ có 1 sprite ly đặc, đặt nước ở order thấp hơn ly và dựa vào mask.

---

## 4. Dựng cây GameObject

```
Brewing (empty, gắn BrewingManager)
└── Cup (empty, gắn CupController + DrinkController)
    ├── CupBack        SpriteRenderer  (sprite ly, order = CupBack) — tùy chọn
    ├── CavityMask     SpriteMask      (sprite = silhouette lòng ly)
    ├── Liquid         (gắn LiquidController + LiquidWobble)   ← liquidRoot
    │   ├── LiquidBody    SpriteRenderer  pivot ĐÁY, maskInteraction = Visible Inside Mask
    │   └── LiquidSurface SpriteRenderer  (ellipse), maskInteraction = Visible Inside Mask
    ├── ToppingContainer (empty)  ← cha của topping rơi vào, cũng nên bị mask
    ├── CavityWalls    PolygonCollider2D (static) ôm theo lòng ly để giữ topping
    ├── MouthTrigger   Collider2D (isTrigger) phủ miệng ly — phát hiện thả topping
    ├── PourTarget     (empty) đặt ở tâm miệng ly
    ├── StirZone       Collider2D (isTrigger) phủ vùng ly để vuốt khuấy
    └── CupFront       SpriteRenderer (thành trước ly, order = CupFront) — tùy chọn

Bottles (empty)
├── BottleTea     SpriteRenderer + Collider2D + IngredientBottle  (+ child "Spout")
├── BottleMilk    …
└── BottleSoda    …  (mỗi chai 1 PourStream riêng hoặc dùng chung)

PourStreamFx (gắn PourStream)
├── StreamSprite  SpriteRenderer pivot ĐỈNH
├── Splash        ParticleSystem
└── Bubbles       ParticleSystem

ToppingTray (empty)
├── PearlSpawner  ToppingSpawner (toppingPrefab = prefab trân châu)
├── JellySpawner  ToppingSpawner
├── FruitSpawner  ToppingSpawner
└── IceSpawner    ToppingSpawner

Stir (empty, gắn StirController)   — hoặc gắn chung trên Cup
UI Canvas
├── Btn_NextCup / Btn_PrevCup → CupSelector.Next / Prev
├── Btn_Start    → BrewingManager.BeginMixing
├── Btn_Drink    → BrewingManager.BeginDrinking
└── Btn_New      → BrewingManager.StartNewCup
```

---

## 5. Gán reference từng component

### LiquidController (trên `Liquid`)
- **Body** = `LiquidBody` (sprite 1 unit, **pivot Bottom**).
- **Surface** = `LiquidSurface`.
- **bottomLocalY / cavityHeight / bodyWidth**: căn theo lòng ly. Bật chọn object để thấy gizmo
  đường cyan (đáy & miệng vùng chứa nước) rồi chỉnh cho khớp sprite ly.
- `emptyColor` alpha = 0 (ly trống trong suốt).
- Cả Body và Surface: **Sprite Renderer → Mask Interaction = Visible Inside Mask**.

### CavityMask (SpriteMask)
- `sprite` = hình lòng ly. Nếu không có sprite riêng, dùng luôn sprite ly và bật
  **Custom Range** + chỉnh **Alpha Cutoff** để chỉ phần đặc làm mask.

### LiquidWobble (trên `Liquid`)
- **liquidRoot** = chính object `Liquid` (cha của Body + Surface).
- **shaderTarget** = `LiquidBody` (chỉ cần nếu dùng shader; bỏ trống vẫn chạy).
- stiffness ~120, damping ~6, maxTilt ~7. Tinh chỉnh theo cảm giác.

### CupController (trên `Cup`)
- cupBody = `CupBack` (hoặc sprite ly chính), cavityMask = `CavityMask`,
  liquid = `LiquidController`, wobble = `LiquidWobble`,
  toppingContainer = `ToppingContainer`, mouthTrigger = `MouthTrigger`, pourTarget = `PourTarget`.

### CupSelector
- cup = `CupController`. **cups**: thêm 1 phần tử cho mỗi kiểu ly:
  `cupSprite` (Asset/Cup/…), `maskSprite` (silhouette lòng ly), `cavityBottomLocalY`,
  `cavityHeight`, `liquidWidth`.

### IngredientBottle (mỗi chai)
- cam = Main Camera, cup = `CupController`, stream = `PourStream`, brewAudio = `BrewingAudio`,
  spout = child "Spout" đặt ở vòi chai.
- **liquidColor**: trà = nâu, sữa = trắng kem, soda = hồng/xanh nhạt, syrup = màu trái cây.
- fillRatePerSecond ~0.45, pourRadiusX ~1.2, pourTiltAngle ~-65.

### PourStream
- streamSprite = `StreamSprite` (**pivot Top**), splash = `Splash`, bubbles = `Bubbles`.
- Splash PS: Start Lifetime ngắn (~0.4s), Shape = Cone hướng lên, burst nhẹ, Gravity dương nhỏ.
- Bubbles PS: Start Speed nhỏ hướng lên, Size dao động, Color theo nước.

### Topping prefab (cho mỗi loại: trân châu / jelly / trái cây / đá)
- Gắn: SpriteRenderer (Asset tương ứng) + **Collider2D** (Circle/Polygon) +
  **Rigidbody2D** + `DraggableTopping` + `ToppingBuoyancy`.
- SpriteRenderer **Mask Interaction = Visible Inside Mask** (để topping bị cắt trong ly).
- Rigidbody2D: Gravity Scale để code tự set; Collision Detection = Continuous; Interpolate = Interpolate.
- `ToppingBuoyancy`:
  - **Đá viên**: buoyancy cao (~16), restDepth ~0.1 → nổi lên mặt.
  - **Trân châu**: buoyancy thấp (~6), restDepth ~1.0 → chìm đáy.
  - **Jelly / trái cây**: ở giữa.
- Lưu thành **Prefab**, gán vào `ToppingSpawner.toppingPrefab`.
- Trong `ToppingSpawner` gán cup / cam / brewAudio để inject vào instance (prefab không trỏ
  được object trong scene).

### StirController
- cam, cup, stirZone = `StirZone` (trigger phủ ly), brewAudio.

### DrinkController (trên `Cup`)
- cam, cup, cupRoot = object `Cup`, cupCollider = collider để nắm ly (có thể dùng `MouthTrigger`
  hoặc 1 collider phủ ly), brewAudio.
- liftThreshold ~0.6, drainRatePerSecond ~0.5, maxTilt ~22, liftForFullDrain ~2.5.

### BrewingAudio
- 3 AudioSource: **pourSource**, **sipSource** (loop), **oneShotSource** (one-shot). Tất cả route
  qua **SFX mixer group** (để slider Setting điều khiển được — xem `AudioService`).
- Clip: pourLoop, sipLoop (loop ASMR), plop, stir, finalGulp, bubble. Tạm thời có thể dùng
  `Assets/Audio/SFX/pop.mp3` cho `plop`, sẽ thay clip ASMR sau.

### BrewingManager
- Gán: cupSelector, cup, **bottles[]** (mọi chai), **toppingSpawners[]** (mọi khay),
  stir, drink, brewAudio. scorePerDrink tùy ý.
- Để cộng điểm vào hệ thống score sẵn có: trong `GameBootstrapper` đã có sẵn field
  **brewingManager** (optional) — kéo `BrewingManager` vào đó là xong (đã `BindGameService`).

---

## 6. Luồng chơi

1. **SelectCup**: bấm Next/Prev đổi ly (qua `CupSelector`) → bấm **Start** (`BeginMixing`).
2. **Mixing** (pha): kéo chai rót nước (fill + đổi màu + splash + bubble + ASMR loop),
   kéo topping thả vào ly (bounce + buoyancy + plop), vuốt ngang trên ly để khuấy/lắc.
   Cả 3 thao tác dùng chung khóa kéo nên không tranh nhau 1 ngón tay.
3. Bấm **Drink** (`BeginDrinking`) → kéo ly lên: ly nghiêng, nước rút dần, topping chìm theo,
   phát tiếng hút ASMR. Hết nước → phase **Done**, cộng điểm.
4. Bấm **New** (`StartNewCup`) → reset, quay lại chọn ly.

---

## 7. Về Sprite Mask + shader (đọc nếu nước bị tràn ra ngoài ly)

- Cơ chế chính giữ nước trong ly là **SpriteMask** + `Mask Interaction = Visible Inside Mask`
  trên LiquidBody / LiquidSurface / topping. Wobble dùng **transform** nên chạy được với
  **material sprite mặc định** — bạn KHÔNG bắt buộc dùng shader.
- `LiquidWobble.shader` chỉ là polish (ripple + gradient + gloss). Nếu gán material shader này mà
  mask hỏng trong phiên bản URP của bạn, hãy quay lại material sprite mặc định cho LiquidBody —
  gameplay vẫn đầy đủ.
- Muốn dùng shader: tạo Material từ `FruitMix/LiquidWobble`, gán vào LiquidBody, và để
  `LiquidWobble.shaderTarget` = LiquidBody (nó set `_Splash` theo năng lượng sóng).

---

## 8. Tinh chỉnh "juicy" (gợi ý)

- DOTween fill ease `OutCubic` 0.35s cho cảm giác nước dâng mượt; tăng `AddImpulse` khi thả topping
  để nước sóng mạnh hơn.
- Particle: thêm 1 PS "drop burst" nhỏ gán vào `DraggableTopping.dropBurstPrefab` để có tóe nước
  khi topping chạm mặt.
- Camera: thêm punch nhẹ (DOShakePosition) khi thả đá để tăng phản hồi (có thể bổ sung sau).
- Giữ mọi animation < 0.4s, ease Out* để phản hồi nhanh, đúng tinh thần ASMR satisfying.
```
