# Apartman Yöneticisi — Unity Portu (başlangıç kiti)

`apartman-yoneticisi.html` içindeki oyun mantığının (kiracı, personel, gün
döngüsü) C#'a taşınmış hali. Unity Editor'ü bu ortamda çalıştıramadığım için
sahne/prefab/UI kurulumu yok — bu betikler mantık katmanı, geri kalanını
Unity'de sen bağlayacaksın.

## 1) Proje oluştur

Unity Hub → New Project → **3D (URP)** template (mobil hedefte performans
için URP tercih edilir).

## 2) Script'leri içeri al

Bu klasördeki tüm `.cs` dosyalarını `Assets/Scripts/` altına kopyala.
Bağımlılık sırası yok, hepsini birden sürükleyebilirsin.

- `GameState.cs`, `ApartmentUnit.cs`, `TenantTypes.cs`, `StaffSystem.cs` →
  saf C#, `UnityEngine`'e bağımlı değil (test edilebilir mantık katmanı).
- `GameManager.cs` → gün döngüsünü işleten `MonoBehaviour`.
- `CharacterWalker.cs`, `IsometricCameraRig.cs` → sahne/görsel katman.

## 3) Kamera (izometrik açı)

Main Camera'ya `IsometricCameraRig` bileşenini ekle. Inspector'da
`xAngle=35`, `yAngle=45` bırak — kamera otomatik olarak Whiteout Survival
tarzı ortografik izometrik açıya döner. `orthographicSize` ile ne kadar
alanın göründüğünü ayarla.

## 4) Karakter (Mixamo)

1. [mixamo.com](https://www.mixamo.com)'a Adobe hesabınla giriş yap.
2. Bir karakter seç, sonra **Idle**, **Walking**, ve bir "use item"/"pick up"
   gibi kısa bir etkileşim animasyonu indir (Format: **FBX for Unity**).
3. İndirdiğin FBX'leri `Assets/Characters/` altına sürükle.
4. Karakter modelini seç → Inspector → **Rig** sekmesi →
   Animation Type = **Humanoid** → Apply.
5. Bir **Animator Controller** oluştur, üç state ekle:
   - `Idle` ↔ `Walking` geçişi: bool parametresi `IsWalking`
   - `Interact` state'i: trigger parametresi `Interact`
   (İsimler `CharacterWalker.cs`'teki `isWalkingParam`/`interactTrigger`
   alanlarıyla eşleşmeli, farklı isim kullanırsan Inspector'dan güncelle.)
6. Karakteri sahneye sürükle, `CharacterWalker` bileşenini ekle, Animator'ı
   ve az sonra oluşturacağın `unitMarkers` dizisini Inspector'dan bağla.

## 5) Bina ve daire işaretleri

Basit küplerle ya da Asset Store'dan bir "low poly isometric building" paketiyle
binayı oluştur. Her daire için sahnede boş bir `Transform` (GameObject → Create
Empty) koy, tam o dairenin önüne/altına yerleştir. Bu transformları sırayla
(Daire 1, Daire 2, ...) `CharacterWalker.unitMarkers` dizisine sürükle.
Ayrıca kapıcının boşta beklediği bir nokta için `idleSpot` ata.

## 6) GameManager

Boş bir GameObject oluştur (adı: `GameManager`), `GameManager.cs`'i ekle.
Sahnede tek bir tane olmalı — `GameManager.Instance` üzerinden her yerden
erişilebilir.

Örnek kullanım (bir Button'un `OnClick`'ine bağlanacak basit bir script):

```csharp
public class UnitButton : MonoBehaviour
{
    public int unitId;

    public void OnRepairClicked() => GameManager.Instance.RepairUnit(unitId);
    public void OnAcceptClicked() => GameManager.Instance.AcceptApplicant(unitId);
}
```

## 7) UI

`GameManager.OnDayProcessed` ve `OnLog` event'lerine abone olan bir
`UIBinder` script'i yazıp Canvas/TextMeshPro alanlarını (para, itibar, gün,
log) güncellemen gerekiyor — bu kısmı Unity'de Canvas'ı kurduktan sonra
istersen ben de yazarım, şu an sahne/Canvas objesi olmadığı için bağlayacak
bir şey yok.

## Notlar

- Sayılar/denge (kira, maliyetler, seviye eğrisi) web sürümüyle birebir
  aynı — `apartman-yoneticisi.html` içinde 45+ günlük otomatik oynatmayla
  test edip dengelemiştim, o değerleri koru istersen.
- Kaydetme (save/load) burada yok; istersen `GameState`'i JSON'a çevirip
  `PlayerPrefs` veya bir dosyaya yazan küçük bir `SaveSystem.cs` da
  ekleyebilirim.
