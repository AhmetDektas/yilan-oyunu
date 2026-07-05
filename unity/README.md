# Orman Otel — Unity Portu (başlangıç kiti)

`apartman-yoneticisi.html`'in güncel "Orman Otel" sürümünün (misafir/oda
yönetimi, personel, olanaklar, başarımlar, **ve** gerçek zamanlı odun
kesme/avlanma haritası) C#'a taşınmış hali. Unity Editor'ü bu ortamda
çalıştıramadığım için sahne/prefab/Canvas nesnelerini elle kuramadım —
bu script'ler mantık + davranış katmanı, sahneyi aşağıdaki adımlarla sen
kuracaksın.

**Önemli tasarım notu:** Harita script'lerini (`PlayerController`,
`Animal`, `ResourceTree`, `DropZone`) **2D top-down** (`Physics2D`,
`SpriteRenderer`, `Vector2`) olarak yazdım — web prototipinde test ettiğim
mantığın birebir karşılığı bu. Gerçekten 3D bir Mixamo karakteri + izometrik
kamera açısıyla göstermek istersen, `PlayerController`/`Animal` içindeki
`Vector2`/`Physics2D`/`SpriteRenderer` kullanımlarını `Vector3`/`Physics`/
`Animator`'a çevirmen yeterli — oyun mantığı (`GameManager`, `StaffSystem`,
vb.) render katmanından tamamen bağımsız, hiç değişmesine gerek yok. 4)
ve 5) adımlarında bunun nasıl yapılacağı anlatılıyor.

## 1) Proje oluştur

Unity Hub → New Project → **2D (URP)** template (harita 2D top-down
olduğu için; 3D'ye geçersen 3D (URP) seç, script notları aynı kalır).

## 2) Script'leri içeri al

Bu klasördeki tüm `.cs` dosyalarını `Assets/Scripts/` altına kopyala.

**Mantık katmanı** (UnityEngine'e bağımlı değil, saf C#):
`GameState.cs`, `RoomUnit.cs`, `GuestTypes.cs`, `StaffSystem.cs`,
`AmenitySystem.cs`, `Achievements.cs`

**Oyun döngüsü:** `GameManager.cs` — tek `MonoBehaviour`, gün döngüsü +
tüm oyuncu aksiyonları (`RepairUnit`, `AcceptBooking`, `HireStaff`,
`BuyAmenity`, `ExpandHotel`, `DepositWood/Meat`, `AnimalRaid`...) burada.

**Harita (gerçek zamanlı toplama):** `PlayerController.cs`,
`ResourceTree.cs`, `Animal.cs`, `DropZone.cs`, `IsometricCameraRig.cs`

**Canvas UI:** `HUDBinder.cs`, `RoomListUI.cs`, `StaffListUI.cs`,
`AmenityListUI.cs`, `AchievementListUI.cs`, `TabController.cs`

TextMeshPro kullanıyorlar — Unity ilk `TMP_Text` referansı gördüğünde
"Import TMP Essentials" isteyecek, kabul et.

## 3) Harita sahnesini kur

1. Boş bir sahne aç. 2D modundaysan kamera zaten ortografik gelir.
2. **Zemin:** büyük bir `Sprite (Square)`, yeşil renk, ölçek ~10x10.
3. **Otel:** bir kutu/sprite, sahnenin üst tarafına yerleştir. Altına boş
   bir `Transform` koy (adı: `HotelFront`) — hayvanların saldırı hedefi.
4. **Ağaçlar:** 4-5 tane sprite/kutu, her birine `ResourceTree` bileşenini
   ve bir `CircleCollider2D` (Is Trigger **kapalı** — tap-hit-test için
   normal collider yeterli) ekle.
5. **Hayvanlar:** 2-3 tane sprite/kutu, her birine `Animal` bileşeni +
   `CircleCollider2D` + `SpriteRenderer` ekle. Inspector'da:
   - `Hotel Front Marker` → 3. adımdaki `HotelFront` transformunu sürükle
   - `Wander Area Min/Max` → haritanın sınırlarına göre bir dikdörtgen ver
6. **Depo & Yemekhane:** iki kutu/sprite, alt köşelere yerleştir. Her
   birine `DropZone` bileşeni ekle (`Type = Wood` / `Type = Meat`), ve
   `Collider2D`'lerinde **Is Trigger** işaretli olsun.
7. **Karakter (Player):** bir sprite/kutu, `PlayerController` +
   `Collider2D` (Is Trigger kapalı) + `Rigidbody2D` (Body Type =
   **Kinematic**) ekle. `Cam` alanına Main Camera'yı sürükle (boş
   bırakırsan otomatik `Camera.main` kullanır).

Bu kurulumla: haritaya dokun → karakter yürür; ağaca dokun → yürüyüp
keser; hayvana dokun → yürüyüp balta ile saldırır; taşınan odun/et
kapasiteyi (`carryCap`, varsayılan 20) doldurunca Depo/Yemekhane'ye
yürüyüp bırakman gerekir.

### 3D + Mixamo'ya çevirmek istersen

- `PlayerController`/`Animal`'daki `Vector2` alanlarını `Vector3` yap,
  `Physics2D.OverlapPoint` yerine `Physics.Raycast` kullan.
- Karakter GameObject'ine `SpriteRenderer` yerine Mixamo'dan indirdiğin
  (Rig = Humanoid yapılmış) modeli + bir `Animator Controller`
  (`IsWalking` bool, `Interact`/`Attack` trigger) ekle; `TryChop`/
  `TryAttack` içine `animator.SetTrigger(...)` çağrıları ekle.
- Kamera için `IsometricCameraRig.cs`'i Main Camera'ya ekle
  (`xAngle=35`, `yAngle=45`, orthographic kalsın — sadece açı değişiyor,
  "gerçek" perspektif izometrik değil ama mobil strateji oyunlarının
  çoğu da böyle yapıyor).

## 4) GameManager

Boş bir GameObject oluştur (adı: `GameManager`), `GameManager.cs`'i ekle.
Sahnede tek bir tane olmalı.

## 5) Canvas UI kurulumu

Hierarchy → UI → Canvas (Render Mode: Screen Space - Overlay).

**HUD (üst bar):** Canvas altına bir yatay `Panel`, içine 5 `TMP_Text`
(Gün / Para / İtibar / Odun / Et) + bir `Slider` (gün ilerleme çubuğu,
Interactable **kapalı**). Boş bir GameObject'e `HUDBinder.cs` ekle, bu
Text/Slider'ları Inspector'dan sürükle, `Player` alanına sahnedeki
karakteri bağla.

**Oda listesi:** Canvas altına `Scroll View` (UI → Scroll View). Content
altına tek bir "kart" prefabı tasarla: bir `Panel` içinde 3 `TMP_Text`
(başlık/durum/ücret) + 1 `Button` (üzerinde bir `TMP_Text` etiket) —
bunu `RoomCardUI` bileşenini ekleyip prefab yap (`Assets/Prefabs/RoomCard`).
Scroll View'un dışına boş bir GameObject koy, `RoomListUI.cs` ekle,
`Content Parent` → Scroll View'un Content'i, `Room Card Prefab` → yeni
prefabın.

**Personel / Olanaklar / Başarım listeleri:** aynı desen — her biri için
ayrı bir Scroll View + kart prefabı (`StaffCardUI` / `AmenityCardUI` /
`AchievementCardUI` bileşenleriyle) + liste script'i
(`StaffListUI` / `AmenityListUI` / `AchievementListUI`).

**Sekme geçişi:** Alt tarafa 5 `Button` (Harita/Otel/Personel/Olanaklar/
Başarım). Boş bir GameObject'e `TabController.cs` ekle, `Panels` dizisine
5 ana paneli (Harita sahnesi kökü dahil, veya haritayı ayrı bir Canvas
katmanında tutup sadece diğer 4'ü panel yap) aynı sırayla sürükle. Her
butonun `OnClick`'ine `TabController.ShowTab(i)` çağrısını bağla (`i`:
0=Harita, 1=Otel, 2=Personel, 3=Olanaklar, 4=Başarım).

Örnek bir buton callback'i (oda kartı dışında, tekil aksiyonlar için):

```csharp
public class SimpleActionButton : MonoBehaviour
{
    public void OnExpandClicked() => GameManager.Instance.ExpandHotel();
    public void OnForceDayClicked() => GameManager.Instance.ForceCompleteDay();
}
```

## Notlar

- Sayılar/denge (kira, maliyetler, seviye eğrileri, avcı verimi, yemekhane
  fiyatı) web sürümüyle birebir aynı — `apartman-yoneticisi.html`'de
  60 günlük otomatik oynatmayla test edip dengelemiştim.
- Taşınan (carry) odun/et `PlayerController` üzerinde tutulur, kalıcı
  kayda dahil değil — sahne yeniden yüklenince sıfırlanır (kasıtlı,
  basitlik için). `GameState.wood`/`meat` (depolanmış stok) ise kalıcı
  olması gereken asıl ekonomi verisi.
- Kaydetme (save/load) yok; istersen `GameState`'i JSON'a çevirip
  `PlayerPrefs`'e yazan küçük bir `SaveSystem.cs` da ekleyebilirim.
- Duvar/okçu kulesi savunma yapıları henüz yok — istersen bir sonraki
  adımda `Wall.cs`/`ArcherTower.cs` ekleriz (kule, menzildeki hayvanlara
  periyodik hasar verir, `GuvenlikFactor()` ile aynı mantığa entegre
  olur).
