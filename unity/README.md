# Orman Otel — Unity Projesi

"Orman Otel": misafir/oda yönetimi, personel, olanaklar, başarımlar,
gerçek zamanlı odun kesme/avlanma haritası, Papers-Please tarzı kimlik
kontrolü, ve duvar/okçu kulesi savunması olan bir otel işletme +
hayatta kalma oyunu. Bu repo artık sadece bu Unity projesine ayrılmış
durumda (eski web prototipi kaldırıldı). Unity Editor'ü bu ortamda
çalıştıramadığım için sahne/prefab/Canvas nesnelerini elle kuramadım —
script'ler mantık + davranış katmanı, sahneyi aşağıdaki adımlarla sen
kuracaksın.

**Kontrol şeması:** Dokunarak hareket/etkileşim **yok** — hiçbir tap/
raycast bulunmuyor, bu yüzden Canvas UI butonlarıyla (Kabul Et, sekme
butonları vb.) asla çakışmıyor. Hareket bir **joystick** ile (kamera
göreceli, `NavMeshAgent.velocity` üzerinden — karakter ve hayvanlar yine
ağaçların/binanın içinden geçmiyor, gerçek yol buluyor). Ağaç kesme,
hayvan avlama ve kule/depo/yemekhane gibi satın alma/bırakma işlemleri
**tamamen yakınlık bazlı**: karakter menzile girince kendiliğinden
tetikleniyor, ekstra bir dokunma gerekmiyor.

## 1) Proje oluştur

Unity Hub → New Project → **3D (URP)** template.

## 2) Script'leri içeri al

Bu repodaki `unity/Assets/Scripts/` klasörünün tamamını, kendi Unity
projenin `Assets/Scripts/` klasörüne kopyala (klasör yapısı zaten
birebir eşleşiyor, olduğu gibi sürükleyip bırakabilirsin).

**Mantık katmanı** (UnityEngine'e bağımlı değil, saf C#):
`GameState.cs`, `RoomUnit.cs`, `GuestTypes.cs`, `StaffSystem.cs`,
`AmenitySystem.cs`, `Achievements.cs`, `GuestDocument.cs`

**Misafir portresi:** `PortraitGenerator.cs` — dışarıdan hiçbir resim
almadan, çalışma anında kodla bir yüz silüeti çizip `Sprite`'a çeviren
yardımcı sınıf; `GuestCheckPanel.cs` bunu kimlik kontrol panelindeki
portre için kullanır.

**Oyun döngüsü:** `GameManager.cs` — tek `MonoBehaviour`, gün döngüsü +
tüm oyuncu aksiyonları (`RepairUnit`, `AcceptBooking`, `ResolveEntryDecision`,
`HireStaff`, `BuyAmenity`, `ExpandHotel`, `DepositWood/Meat`, `AnimalRaid`...)
burada.

**Harita (gerçek zamanlı toplama, NavMeshAgent + joystick tabanlı):**
`PlayerController.cs`, `Joystick.cs` (dokunmalı sanal joystick UI'ı),
`ResourceTree.cs`, `Animal.cs`, `DropZone.cs`, `IsometricCameraRig.cs`,
`CameraFollow.cs`

**Savunma:** `Wall.cs` — canı (HP) olan **gerçek fiziksel duvar**.
Yaklaşan hayvanlar oteli değil önce en yakın canlı duvarı hedefler, ona
saldırır; duvar yıkılırsa (HP 0) o bölgeden geçip otele ulaşabilirler.
Yıkık duvarın yanına odun taşıyarak yürümek otomatik tamir eder (Depo/
Yemekhane gibi, dokunma gerekmez). `ArcherTower.cs` (otomatik saldıran,
öldürdüğü hayvanlardan et biriktiren kule), `TowerBuildSite.cs` (parayla
inşa edilen kule alanı), `TowerPersistence.cs` (kuleleri oturumlar
arasında kaydeder/geri yükler). Olanaklar'daki "Çit & Duvar" seviyesi
ise `Wall.cs`'ten ayrı, tamamlayıcı bir katman — hayvanların hiç
yaklaşmaya **karar verme ihtimalini** ve otele ulaştıklarında verdikleri
hasarı azaltan soyut bir çarpan (`AmenitySystem`/`AmenityListUI`).

**Canvas UI:** `HUDBinder.cs`, `RoomListUI.cs`, `StaffListUI.cs`,
`AmenityListUI.cs`, `AchievementListUI.cs`, `GuestCheckPanel.cs`,
`GameOverPanel.cs`, `TabController.cs`, `CarryFullToast.cs` (taşıma
kapasitesi dolunca uyarı), `TutorialPanel.cs` (ilk açılışta bir kerelik
"nasıl oynanır" paneli)

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

   Not: hayvan "saldırı" moduna geçtiğinde önce otele değil, en yakın
   **canlı duvara** (aşağıdaki 5b) yönelir — duvar yoksa/hepsi yıkıksa
   doğrudan `Hotel Front Marker`'a gidip yağma yapar.
5b. **Duvarlar:** Otelin çevresini çevreleyecek şekilde birkaç küp/model
   (duvar segmenti) yerleştir. Her birine şunları ekle:
   - `Wall.cs` bileşeni
   - `NavMeshObstacle` (Carve **açık**) — duvar sağlamken yolu gerçekten
     kapatır, yıkılınca (`hp <= 0`) `Wall.cs` bunu otomatik kapatır ve
     hayvanlar oradan geçebilir hale gelir (NavMesh'i yeniden bake etmene
     gerek yok)
   - Trigger **olmayan** bir `Collider` — hayvanların/oyuncunun
     `Physics.OverlapSphere` ile duvarı bulabilmesi için
   Bu duvarlar arasında boşluk bırakma; hayvanların "hiç duvar yok"
   sayıp direkt otele gitmemesi için otel çevresini olabildiğince
   kapatacak şekilde diz. Yıkılan bir duvarın yanına odun taşıyarak
   yürümek onu otomatik tamir eder (Depo/Yemekhane gibi, dokunma
   gerekmez — bkz. `Wall.repairWoodCost`).
6. **Depo & Yemekhane:** iki küp/model, alt köşelere yerleştir. Her
   birine `DropZone` bileşeni ekle (`Type = Wood` / `Type = Meat`), ve
   Collider'da **Is Trigger** işaretli olsun.
7. **Karakter (Player):** bir küp/model (ya da 3D + Mixamo karakteri,
   aşağıya bak), `PlayerController` bileşeni ekle — `NavMeshAgent` ve
   `Rigidbody` gereksinimlerini otomatik ekleyecek (Rigidbody'yi
   Kinematic yapman `Awake()` içinde otomatik oluyor). `Cam` alanına
   Main Camera'yı sürükle.
7b. **Sanal joystick (UI):** Canvas altına bir `Panel` (arka plan
   dairesi, adı `Background`) + içine bir `Image` (tutamaç, adı
   `Handle`) koy, ekranın sol-alt köşesine sabitle (Anchor: bottom-left).
   `Background`'a `Joystick.cs` bileşenini ekle, `Background`/`Handle`
   alanlarını sürükle. `PlayerController.Joystick` alanına bu bileşeni
   bağla. Artık dokunup sürüklemek karakteri kamera yönüne göre hareket
   ettirir; parmağı bırakınca joystick merkeze döner ve karakter durur.
   Ağaç kesme/hayvan avlama **otomatik** — karakter menzile girince
   kendiliğinden tetiklenir, ekstra dokunma gerekmez.
8. **Kamera takibi:** Main Camera'ya hem `IsometricCameraRig`
   (`xAngle=35`, `yAngle=45` — açıyı ayarlar) hem de `CameraFollow`
   (karakteri takip eder) bileşenlerini ekle. Kamerayı Scene view'da elle
   sürükleyip açıyı/uzaklığı beğendiğin yere getir, `CameraFollow.Target`
   alanına karakteri sürükle, sonra Inspector'da `CameraFollow`
   bileşeninin sağ üstündeki ⋮ menüsünden (veya bileşen başlığına sağ
   tık) **"Offset'i Şu Anki Konumdan Hesapla"**'ya bas — kamera artık o
   bağıl konumu koruyarak karakteri takip eder. `Target` boş bırakılırsa
   sahnedeki `PlayerController`'ı otomatik bulur.
9. **Okçu kulesi (opsiyonel, savunma):** Bir kule modeli/küp hazırla,
   `ArcherTower` bileşeni ekle (Collider = trigger, menzil kadar büyük).
   Bunu bir **prefab** yap (`Assets/Prefabs/ArcherTower`), sahneden sil —
   gerçek kuleler oyun içinde `TowerBuildSite` üzerinden inşa edilecek.
   Otelin çevresine 1-3 tane boş kutu/marker yerleştir, her birine
   `TowerBuildSite` bileşeni ekle (Collider = trigger), `Archer Tower
   Prefab` alanına az önce yaptığın prefabı sürükle, `Cost` belirle
   (varsayılan öneri: 4000₺), ve **her birine benzersiz bir `Site Id`**
   yaz (örn. `"tower_kuzey"`, `"tower_dogu"`) — bu, kule kalıcılığı için
   şart. Bu marker'lara **Static işaretleme**, çünkü henüz inşa
   edilmediler.
10. **Kule kalıcılığı:** `GameManager`'ın olduğu GameObject'e (veya ayrı
    bir boş GameObject'e) `TowerPersistence.cs`'i ekle, `Archer Tower
    Prefab` alanına 9. adımdaki prefabı sürükle. Bu sayede inşa ettiğin
    kuleler (konum + biriken et) oturumlar arasında (uygulama kapanıp
    açıldığında) korunur — hiçbir ek Canvas/UI kurulumu gerekmiyor,
    otomatik çalışır.
11. **NavMesh bake et:** Window → AI → Navigation → Bake sekmesi → Bake
    butonu. Zemin Walkable, ağaç/otel/hayvanlar Not Walkable olarak
    görünmeli (mavi alan = yürünebilir).

Bu kurulumla: joystick'i sürükle → karakter kamera yönüne göre **yol
bularak** yürür (ağaç/bina gibi engellerin içinden geçmez); bir ağacın
yanına gel → otomatik keser; bir hayvanın yanına gel → otomatik balta ile
saldırır; taşınan odun/et kapasitesi (`carryCap`, varsayılan 20) dolunca
Depo/Yemekhane'ye yürüyüp bırakman gerekir (o da otomatik, bölgeye
girince tetiklenir). Bir kule inşa alanına yürürsen (parayı karşılarsan)
otomatik olarak orada bir okçu kulesi doğar; kule kendi kendine
menzilindeki hayvanlara ateş eder, öldürdüğü hayvanlardan biriken eti
almak için kulenin yanına yürümen yeterli. **Hiçbir adımda dokunarak
hedef seçmek/tıklamak yok** — sadece joystick ile hareket.

### 3D karakter + Mixamo eklemek istersen

Küp yerine gerçek bir karakter modeli kullanmak için:

1. [mixamo.com](https://www.mixamo.com)'dan bir karakter + **Idle**,
   **Walking**, kısa bir **Attack**/**Use Item** animasyonu indir
   (Format: FBX for Unity).
2. Modeli sahneye sürükle, Rig sekmesinde Animation Type = **Humanoid**.
3. Bir Animator Controller oluştur: `IsWalking` bool (Idle↔Walking),
   isteğe bağlı bir `Interact`/`Attack` trigger — isimler
   `PlayerController.isWalkingParam` ile eşleşsin (Inspector'dan
   değiştirebilirsin).
4. `PlayerController`'ın `animator` alanına bu Animator'ı bağla. Yürüme
   zaten otomatik senkronize; kesme/vuruşta da bir animasyon tetiklemek
   istersen `TryAutoChop`/`TryAutoAttack` içine (ilgili miktar
   eklendikten sonra) `animator.SetTrigger("Interact")` gibi tek satırlık
   bir çağrı eklemen yeterli.
5. Kamera için `IsometricCameraRig.cs`'i Main Camera'ya ekle
   (`xAngle=35`, `yAngle=45`) — Whiteout Survival tarzı izometrik açı.
   `CameraFollow.cs`'i de ekleyip karaktere bağlaman gerekiyor (3.
   bölümdeki 8. adıma bak) — yoksa kamera sabit kalır, karakter kadraj
   dışına çıkar.

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
kapalı bir `Panel` (`GuestCheckPanel` GameObject'i): içine bir **portre**
için bir `Image` (kare/dikdörtgen, örn. 128x128), misafir adı, tipi,
"Meslek: ..." ve "Eşya: ..." metinleri için 5 `TMP_Text`, şüpheli
olduğunda görünecek küçük bir uyarı ikonu (`GameObject`, örn. kırmızı
"⚠️"), ve **İçeri Al** / **Reddet** olmak üzere 2 `Button`.
`GuestCheckPanel.cs`'i bu panelin köküne ekle, alanları bağla
(`panelRoot` = panelin kendisi, `Portrait Image` = az önceki `Image`).
Oda kartındaki "Kimliğini İncele" butonuna bastığında bu panel açılır;
oyuncu portreye/meslek-eşya bilgisine bakıp karar verir — **gerçek
doğru/yanlış (IsTrouble) oyuncudan gizli**, sadece görünür ipuçlarına
(portredeki kaçamak/şaşı bakış ve gözlük gibi detaylar, şüpheli bayrağı,
meslek/eşya uyuşmazlığı) göre tahmin ediyorsun, tıpkı Papers Please'deki
gibi.

**Portreler nereden geliyor?** Hiçbir resim/asset içe aktarmana gerek
yok — `PortraitGenerator.cs` her misafir için küçük bir yüz silüetini
(ten rengi + saç/şapka stili + göz şekli + bazen gözlük) tamamen kodla,
çalışma anında bir `Texture2D`'ye çizip `Sprite`'a çeviriyor.
Varyasyon misafirin adının hash'inden geliyor (aynı isim → hep aynı
yüz), şüpheli olan misafirlerin gözleri ince/kaçamak çiziliyor —
**hiçbir zaman gizli `IsTrouble` bilgisini kullanmıyor**, sadece zaten
görünür olan ipuçlarını (isim, şüpheli bayrağı) okuyor, yoksa oyunun
tahmin mekaniği bozulurdu.

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
`GameManager.Restart()`'ı çağırır — bu, kayıtlı ekonomi + kule
verilerini siler ve **sahneyi baştan yükler** (sadece bellekteki State'i
sıfırlamak yerine), böylece o oturumda inşa edilmiş kuleler de gerçekten
kaybolur ve orijinal inşaat alanları (`TowerBuildSite`) geri gelir.

**Taşıma kapasitesi uyarısı:** Harita ekranının üstüne (görünmez
başlayan) bir `TMP_Text` koy, boş bir GameObject'e `CarryFullToast.cs`
ekle, `Player`/`Toast Text` alanlarını bağla. Odun ya da et
kapasitesi (`carryCap`) dolduğunda 2.5 saniyeliğine bir uyarı metni
belirir, sonra otomatik kaybolur — sessizce hiçbir şey olmaması yerine.

**İlk oynama rehberi:** Canvas altına, varsayılan **açık** bir `Panel`
(`TutorialPanel` GameObject'i) koy — içine joystick/otomatik toplama/
bırakma bölgelerini anlatan sabit metin(ler) ve bir **Anladım** `Button`
yaz. `TutorialPanel.cs`'i ekle, `panelRoot`/`dismissButton` alanlarını
bağla. Bu panel sadece **uygulamanın hiç açılmadığı ilk seferde**
gösterilir (bir `PlayerPrefs` bayrağıyla takip edilir, kayıtlı oyundan
bağımsız); "Anladım"a basınca bir daha çıkmaz.

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

## Süreklilik: oyun neden 60. günde de sıkıcı olmuyor

Gün sayısı ilerledikçe hiçbir şey otomatik zorlaşmıyor olsaydı oyun
belli bir noktadan sonra düz bir çizgiye (para birikince hiçbir tehdit
kalmaz) dönerdi. Bunu önlemek için `GameManager`'a gün bazlı bir
**zorluk eğrisi** eklendi:

- `DifficultyFactor()` — gün 0'da 0, `difficultyRampDays` (varsayılan
  40) güne ulaşınca 1 olan, doğrusal artan tek bir katsayı.
- `AnimalAggressionBonus()` — bu katsayıyla `maxAnimalAggressionBonus`
  (varsayılan 0.35) çarpılıp `Animal.approachChance`'e eklenir; yani
  hayvanlar oyun ilerledikçe otele/duvara saldırma kararını daha sık
  alır (`GuvenlikFactor`/`WallFactor` savunmaları hâlâ üstüne çarpımsal
  olarak etki ediyor, yani iyi savunma bu artışı dengeler).
- `TroubleChanceBonus()` — aynı katsayıyla `maxTroubleChanceBonus`
  (varsayılan 0.15) çarpılıp yeni rezervasyonların "sorunlu" olma
  ihtimaline eklenir (`GuestDocumentGenerator.Generate`'e parametre
  olarak geçiyor) — yani kimlik kontrolü de zamanla daha riskli hâle
  gelir, dikkatsizleşmeye karşı bir fren.
- **Sürü gecesi (opsiyonel):** `animalPrefab`/`hotelFrontMarker`/
  `animalSpawnAreaMin`/`Max` alanlarını doldurursan, her
  `swarmNightEveryDays` (varsayılan 10) günde bir `swarmNightExtraAnimals`
  (varsayılan 2) tane ekstra hayvan otelin çevresinde doğar — düz bir
  zorluk artışı yerine ara sıra sivri bir "baskın gecesi" hissi katar.
  Alanları boş bırakırsan bu özellik tamamen devre dışı kalır, kurulum
  zorunlu değil.

Bütün eşik değerler Inspector'dan ayarlanabilir; oyunun "60 gün" gibi
sabit bir bitişi yok — zorluk `difficultyRampDays`'ten sonra da (1
katsayısında) sabitlenip sürüyor, yani asıl hedef en yüksek günü/parayı
görmek.

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
  oyunu yükler, yoksa sıfırdan başlar. `Restart()` her iki kaydı da silip
  sahneyi yeniden yükler. Herhangi bir Canvas kurulumu gerektirmiyor,
  otomatik çalışıyor.
- **Okçu kuleleri ayrıca kaydediliyor:** `GameState`'in parçası değiller
  (sahne nesneleri oldukları için) — `TowerPersistence.cs` aynı
  otomatik-kayıt ritmiyle (kendi `OnApplicationPause`/`OnApplicationQuit`/
  zamanlayıcısıyla) her kulenin `siteId`'sini, konumunu ve biriken etini
  ayrı bir `PlayerPrefs` anahtarında saklar; sahne başlarken
  `TowerBuildSite`'ları `siteId` ile eşleştirip o kuleleri yeniden inşa
  eder.
- **Kayıt şeması göçü:** `SaveSystem.Load()` yükledikten sonra
  `StaffKey`/`UpgradeKey`'de eksik olan anahtarları varsayılanlarla
  dolduruyor (`BackfillMissingKeys`) — ileride bu enum'lara yeni bir
  değer eklersen (tıpkı `Wall`'ı sonradan eklediğimiz gibi), eski bir
  kayıt dosyası o yeni anahtarı içermese bile `KeyNotFoundException`
  fırlatmaz.
- İncelediğim diğer KaganAyten repoları (`RestaurantGame3DUnity`'nin
  malzeme taşıma/teslim deseni, `Vibe-Survivors`'ın dolaşan düşman
  yapay zekası) zaten kavramsal olarak `PlayerController`/`DropZone`/
  `Animal` tasarımımıza yansıdı; `Unity-Isometric-Procedural-Map-Generator`
  çok az belgelenmiş (2 commit) olduğu için şimdilik entegre etmedim.
- **Fiziksel duvar + savunma çarpanı birlikte çalışıyor:** `Wall.cs`
  gerçek bir engel — canı (HP) var, `NavMeshObstacle` ile yolu fiilen
  kapatıyor. Saldırı moduna geçen bir hayvan önce en yakın **canlı**
  duvarı hedefleyip ona vuruyor (`Animal.FindNearestLiveWall()`); duvar
  yıkılınca (`hp <= 0`) o bölgeden geçip otele ulaşabiliyor. Yıkık bir
  duvarın yanına odun taşıyarak yürümek onu otomatik tamir ediyor
  (dokunma gerekmez, Depo/Yemekhane ile aynı mantık). Bunun yanında
  "Çit & Duvar" Olanaklar sekmesinde para ile seviye atlayan **ayrı,
  soyut** bir savunma çarpanı da var (`AmenitySystem.WallFactor` —
  hayvanın hiç saldırı moduna **karar verme ihtimalini** ve otele
  ulaştığında verdiği yağma hasarını azaltır, `GuvenlikFactor()` ile aynı
  mantıkta çarpımsal olarak birleşiyor) — yani iki katman tamamlayıcı:
  biri "hiç gelmesin", diğeri "gelirse önce duvarı kırsın". Okçu
  kuleleri (`ArcherTower`) ise haritada `TowerBuildSite`'a yürüyüp
  parayla inşa edilen, menzilindeki hayvanlara otomatik ateş eden,
  öldürdüğü hayvanlardan et biriktiren yapılar — biriken eti almak için
  kulenin yanına yürüyüp beklemen yeterli (Depo/Yemekhane'nin tersi
  yönde çalışan aynı "git ve al" ritmi).
