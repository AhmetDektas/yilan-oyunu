# Orman Otel — Unity Projesi

"Orman Otel": misafir/oda yönetimi, personel, olanaklar, başarımlar,
gerçek zamanlı odun kesme/avlanma haritası, Papers-Please tarzı kimlik
kontrolü, ve duvar/okçu kulesi savunması olan bir otel işletme +
hayatta kalma oyunu. Bu repo artık sadece bu Unity projesine ayrılmış
durumda (eski web prototipi kaldırıldı). Unity Editor'ü bu ortamda
çalıştıramadığım için sahne/prefab/Canvas nesnelerini elle kuramadım —
script'ler mantık + davranış katmanı, sahneyi aşağıdaki adımlarla sen
kuracaksın.

**Referans:** Karakter hareketi, github.com/KaganAyten/Click-MoveSource
reposundaki yaklaşımdan uyarlandı (raycast → `NavMeshAgent.SetDestination`
→ animator senkronu) — bu yüzden script'ler gerçek 3D/`NavMeshAgent`
tabanlı: karakter ve hayvanlar ağaçların/binanın içinden geçmiyor, gerçek
yol buluyor.

## 1) Proje oluştur

Unity Hub → New Project → **3D (URP)** template.

## 2) Script'leri içeri al

Bu repodaki `unity/Assets/Scripts/` klasörünün tamamını, kendi Unity
projenin `Assets/Scripts/` klasörüne kopyala (klasör yapısı zaten
birebir eşleşiyor, olduğu gibi sürükleyip bırakabilirsin).

**Mantık katmanı** (UnityEngine'e bağımlı değil, saf C#):
`GameState.cs`, `RoomUnit.cs`, `GuestTypes.cs`, `StaffSystem.cs`,
`AmenitySystem.cs`, `Achievements.cs`, `GuestDocument.cs`

**Oyun döngüsü:** `GameManager.cs` — tek `MonoBehaviour`, gün döngüsü +
tüm oyuncu aksiyonları (`RepairUnit`, `AcceptBooking`, `ResolveEntryDecision`,
`HireStaff`, `BuyAmenity`, `ExpandHotel`, `DepositWood/Meat`, `AnimalRaid`...)
burada.

**Harita (gerçek zamanlı toplama, NavMeshAgent tabanlı):**
`PlayerController.cs`, `ResourceTree.cs`, `Animal.cs`, `DropZone.cs`,
`IsometricCameraRig.cs`

**Savunma:** `ArcherTower.cs` (otomatik saldıran, öldürdüğü hayvanlardan
et biriktiren kule), `TowerBuildSite.cs` (parayla inşa edilen kule
alanı). "Çit & Duvar" ise ayrı bir script değil — mevcut Olanaklar
(`AmenitySystem`/`AmenityListUI`) sistemine eklenen bir seviye, hayvan
yaklaşma ihtimalini ve saldırı hasarını azaltıyor.

**Canvas UI:** `HUDBinder.cs`, `RoomListUI.cs`, `StaffListUI.cs`,
`AmenityListUI.cs`, `AchievementListUI.cs`, `GuestCheckPanel.cs`,
`GameOverPanel.cs`, `TabController.cs`

TextMeshPro kullanıyorlar — Unity ilk `TMP_Text` referansı gördüğünde
"Import TMP Essentials" isteyecek, kabul et.

## 3) Harita sahnesini kur

1. Boş bir 3D sahne aç.
2. **Zemin:** bir `Plane`, yeşil materyal, ölçek ~10x10. Inspector'da
   **Static** kutusunu işaretle (veya en azından Navigation Static).
3. **Otel:** bir küp/basit model, sahnenin üst tarafına yerleştir. Önüne
   boş bir `Transform` koy (adı: `HotelFront`) — hayvanların saldırı
   hedefi.
4. **Ağaçlar:** 4-5 tane küp/basit model (ya da Asset Store'dan düşük
   poligonlu ağaç), her birine `ResourceTree` bileşeni ekle. **Static**
   işaretle (NavMesh bake'te engel olarak sayılsın).
5. **Hayvanlar:** 2-3 tane küp/model, her birine `Animal` bileşeni +
   `NavMeshAgent` ekle. Inspector'da:
   - `Hotel Front Marker` → 3. adımdaki `HotelFront` transformunu sürükle
   - `Wander Area Min/Max` → haritanın sınırlarına göre bir dikdörtgen
     (x/z koordinatları, y'yi zeminin yüksekliğinde sabit tut)
6. **Depo & Yemekhane:** iki küp/model, alt köşelere yerleştir. Her
   birine `DropZone` bileşeni ekle (`Type = Wood` / `Type = Meat`), ve
   Collider'da **Is Trigger** işaretli olsun.
7. **Karakter (Player):** bir küp/model (ya da 3D + Mixamo karakteri,
   aşağıya bak), `PlayerController` bileşeni ekle — `NavMeshAgent` ve
   `Rigidbody` gereksinimlerini otomatik ekleyecek (Rigidbody'yi
   Kinematic yapman `Awake()` içinde otomatik oluyor). `Cam` alanına
   Main Camera'yı sürükle (boş bırakırsan otomatik `Camera.main`
   kullanır).
8. **Okçu kulesi (opsiyonel, savunma):** Bir kule modeli/küp hazırla,
   `ArcherTower` bileşeni ekle (Collider = trigger, menzil kadar büyük).
   Bunu bir **prefab** yap (`Assets/Prefabs/ArcherTower`), sahneden sil —
   gerçek kuleler oyun içinde `TowerBuildSite` üzerinden inşa edilecek.
   Otelin çevresine 1-3 tane boş kutu/marker yerleştir, her birine
   `TowerBuildSite` bileşeni ekle (Collider = trigger), `Archer Tower
   Prefab` alanına az önce yaptığın prefabı sürükle, `Cost` belirle
   (varsayılan öneri: 4000₺). Bu marker'lara **Static işaretleme**,
   çünkü henüz inşa edilmediler.
9. **NavMesh bake et:** Window → AI → Navigation → Bake sekmesi → Bake
   butonu. Zemin Walkable, ağaç/otel/hayvanlar Not Walkable olarak
   görünmeli (mavi alan = yürünebilir).

Bu kurulumla: haritaya dokun → karakter oraya **yol bularak** yürür;
ağaca dokun → yürüyüp keser; hayvana dokun → kovalayıp balta ile
saldırır; taşınan odun/et kapasiteyi (`carryCap`, varsayılan 20)
doldurunca Depo/Yemekhane'ye yürüyüp bırakman gerekir. Bir kule inşa
alanına yürürsen (parayı karşılarsan) otomatik olarak orada bir okçu
kulesi doğar; kule kendi kendine menzilindeki hayvanlara ateş eder,
öldürdüğü hayvanlardan biriken eti almak için kulenin yanına yürümen
yeterli.

### 3D karakter + Mixamo eklemek istersen

Küp yerine gerçek bir karakter modeli kullanmak için:

1. [mixamo.com](https://www.mixamo.com)'dan bir karakter + **Idle**,
   **Walking**, kısa bir **Attack**/**Use Item** animasyonu indir
   (Format: FBX for Unity).
2. Modeli sahneye sürükle, Rig sekmesinde Animation Type = **Humanoid**.
3. Bir Animator Controller oluştur: `IsWalking` bool (Idle↔Walking),
   `Interact` trigger — isimler `PlayerController.isWalkingParam` ile
   eşleşsin (Inspector'dan değiştirebilirsin).
4. `PlayerController`'ın `animator` alanına bu Animator'ı bağla —
   `TryChop`/`TryAttack` içine `animator.SetTrigger("Interact")` gibi
   bir çağrı eklemen tam bir "kesiyor/vuruyor" animasyonu tetikler
   (şu an sadece yürüme senkronize, saldırı/kesme animasyon tetiği
   opsiyonel bir satırla eklenebilir).
5. Kamera için `IsometricCameraRig.cs`'i Main Camera'ya ekle
   (`xAngle=35`, `yAngle=45`) — Whiteout Survival tarzı izometrik açı.

## 4) GameManager

Boş bir GameObject oluştur (adı: `GameManager`), `GameManager.cs`'i ekle.
Sahnede tek bir tane olmalı.

## 5) Canvas UI kurulumu

Hierarchy → UI → Canvas (Render Mode: Screen Space - Overlay).

**HUD (üst bar):** 5 `TMP_Text` (Gün/Para/İtibar/Odun/Et) + bir `Slider`
(gün ilerleme, Interactable kapalı). `HUDBinder.cs` ekle, alanları bağla,
`Player` alanına sahnedeki karakteri sürükle.

**Oda listesi:** `Scroll View` → Content'e `RoomCardUI` bileşenli bir kart
prefabı (3 `TMP_Text` + 1 `Button`). `RoomListUI.cs`'i boş bir
GameObject'e ekle, `Content Parent`/`Room Card Prefab`'ı bağla, ve
**`Guest Check Panel` alanına aşağıdaki paneli bağla.**

**Kimlik kontrol paneli (Papers-Please):** Canvas altına, varsayılan
kapalı bir `Panel` (`GuestCheckPanel` GameObject'i): içine misafir adı,
tipi, "Meslek: ..." ve "Eşya: ..." metinleri için 5 `TMP_Text`, şüpheli
olduğunda görünecek küçük bir uyarı ikonu (`GameObject`, örn. kırmızı
"⚠️"), ve **İçeri Al** / **Reddet** olmak üzere 2 `Button`.
`GuestCheckPanel.cs`'i bu panelin köküne ekle, alanları bağla
(`panelRoot` = panelin kendisi). Oda kartındaki "Kimliğini İncele"
butonuna bastığında bu panel açılır; oyuncu meslek/eşya bilgisine bakıp
karar verir — **gerçek doğru/yanlış (IsTrouble) oyuncudan gizli**, sadece
görünür ipuçlarına (şüpheli bayrağı, meslek/eşya uyuşmazlığı) göre tahmin
ediyorsun, tıpkı Papers Please'deki gibi.

**Personel / Olanaklar / Başarım listeleri:** aynı desen — her biri için
ayrı bir Scroll View + kart prefabı (`StaffCardUI` / `AmenityCardUI` /
`AchievementCardUI`) + liste script'i (`StaffListUI` / `AmenityListUI` /
`AchievementListUI`).

**Sekme geçişi:** Alt tarafa 5 `Button` (Harita/Otel/Personel/Olanaklar/
Başarım). `TabController.cs` ekle, `Panels` dizisine 5 ana paneli aynı
sırayla sürükle, her butonun `OnClick`'ine `ShowTab(i)` bağla (`i`:
0=Harita, 1=Otel, 2=Personel, 3=Olanaklar, 4=Başarım).

**İflas / Oyun Bitti paneli:** Canvas altına, varsayılan kapalı bir
`Panel` (örn. `GameOverPanel` GameObject'i): bir `TMP_Text` (sonuç mesajı)
+ bir **Yeniden Başla** `Button`. `GameOverPanel.cs`'i bu panelin köküne
ekle, `panelRoot`/`messageText`/`restartButton` alanlarını bağla. Para 3
gün üst üste eksiye düşerse (`GameManager.IsGameOver` true olur, gün
döngüsü durur) bu panel otomatik açılır; **Yeniden Başla** butonu
`GameManager.Restart()`'ı çağırıp ekonomiyi sıfırdan kurar.

## Kimlik kontrolü nasıl dengelendi

`GuestDocument.cs`'teki `GuestDocumentGenerator`:
- Gelen misafirlerin **%25'i** gerçekten "sorunlu" (`IsTrouble`).
- Bunların **%70'i** görünür bir ipucu taşır (`IsSuspicious` = şüpheli
  meslek/eşya) — kalan %30'u tertemiz görünür, yakalaman zor.
- Dürüst misafirlerin de **%8'i** rastgele "yanlış alarm" taşır (masum
  ama tuhaf bir eşyayla gelir) — sırf "şüpheli görünen her şeyi reddet"
  stratejisi işe yaramasın diye.
- İçeri alırsan **anında** bir şey olmaz — sorunlu misafir de herkes gibi
  odaya yerleşip kira ödemeye başlar. Kalırken her gün
  `GameManager.troubleStrikeChancePerDay` (varsayılan %25) ihtimalle
  "vurur": kasadan 200-800₺ çalar, itibar -4, **ve odayı boşaltıp kaçar**
  (misafir sayın da düşer). Ne zaman vuracağı belirsiz — bir gün önce mi
  yoksa on gün sonra mı, bilemezsin.
- Doğru reddedersen: itibar +3. Masum birini haksız reddedersen: itibar
  -2 (körü körüne reddetmek de bedelsiz değil).

Sayıları `GuestDocument.cs`'in başındaki `TroubleChance` /
`TroubleTellChance` / `FalseFlagChance` sabitlerinden, vurma sıklığını
ise `GameManager.troubleStrikeChancePerDay`'den ayarlayabilirsin.

## Notlar

- Sayılar/denge (kira, maliyetler, seviye eğrileri, avcı verimi, yemekhane
  fiyatı) geliştirme sırasında bir web prototipinde 60 günlük otomatik
  oynatmayla test edilip dengelenmiş değerler.
- Taşınan (carry) odun/et `PlayerController` üzerinde tutulur, kalıcı
  kayda dahil değil — sahne yeniden yüklenince sıfırlanır (kasıtlı,
  basitlik için). `GameState.wood`/`meat` (depolanmış stok) ise kalıcı
  olması gereken asıl ekonomi verisi.
- **Kaydetme/yükleme var:** `SaveSystem.cs`, `GameState`'i (Dictionary/
  nullable alanlar dahil) düz DTO'lara çevirip Unity'nin yerleşik
  `JsonUtility` + `PlayerPrefs`'i ile saklıyor — ekstra paket gerekmez.
  Otomatik kayıt: her `autosaveIntervalSeconds` (varsayılan 30sn), uygulama
  arka plana atıldığında (`OnApplicationPause`) ve kapanırken
  (`OnApplicationQuit`). `GameManager.Awake()` başlarken önce kayıtlı
  oyunu yükler, yoksa sıfırdan başlar. `Restart()` ve iflas anı kaydı
  siler. Herhangi bir sahne/Canvas kurulumu gerektirmiyor, otomatik
  çalışıyor.
- İncelediğim diğer KaganAyten repoları (`RestaurantGame3DUnity`'nin
  malzeme taşıma/teslim deseni, `Vibe-Survivors`'ın dolaşan düşman
  yapay zekası) zaten kavramsal olarak `PlayerController`/`DropZone`/
  `Animal` tasarımımıza yansıdı; `Unity-Isometric-Procedural-Map-Generator`
  çok az belgelenmiş (2 commit) olduğu için şimdilik entegre etmedim.
- **Duvar/okçu kulesi savunma sistemi var:** "Çit & Duvar" Olanaklar
  sekmesinde para ile seviye atlayan bir savunma çarpanı (hayvan yaklaşma
  ihtimalini ve saldırı hasarını azaltır, `GuvenlikFactor()` ile aynı
  mantıkta çarpımsal olarak birleşiyor). Okçu kuleleri (`ArcherTower`)
  ise haritada `TowerBuildSite`'a yürüyüp parayla inşa edilen, menzilindeki
  hayvanlara otomatik ateş eden, öldürdüğü hayvanlardan et biriktiren
  yapılar — biriken eti almak için kulenin yanına yürüyüp beklemen
  yeterli (Depo/Yemekhane'nin tersi yönde çalışan aynı "git ve al"
  ritmi).
