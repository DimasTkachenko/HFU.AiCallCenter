namespace Hfu.VoiceRegistration.Application.ReferenceData;

public sealed class UkrainianRegionReferenceDataProvider : IRegionReferenceDataProvider
{
    private static readonly IReadOnlyList<RegionReferenceItem> Regions =
    [
        Region("hfu-region-arkrym", "Автономна Республіка Крим", "АР Крим", "Крим", "Автономная Республика Крым", "Крым"),
        Region("hfu-region-vinnytska", "Вінницька область", "Вінницька", "Винницкая область", "Винницкая"),
        Region("hfu-region-volynska", "Волинська область", "Волинська", "Волынская область", "Волынская"),
        Region("hfu-region-dnipropetrovska", "Дніпропетровська область", "Дніпропетровська", "Днепропетровская область", "Днепропетровская"),
        Region("hfu-region-donetska", "Донецька область", "Донецька", "Донецкая область", "Донецкая"),
        Region("hfu-region-zhytomyrska", "Житомирська область", "Житомирська", "Житомирская область", "Житомирская"),
        Region("hfu-region-zakarpatska", "Закарпатська область", "Закарпатська", "Закарпатская область", "Закарпатская"),
        Region("hfu-region-zaporizka", "Запорізька область", "Запорізька", "Запорожская область", "Запорожская"),
        Region("hfu-region-ivano-frankivska", "Івано-Франківська область", "Івано-Франківська", "Ивано-Франковская область", "Ивано-Франковская"),
        Region("hfu-region-kyivska", "Київська область", "Київська область", "Киевская область", "Киевская"),
        Region("hfu-region-kirovohradska", "Кіровоградська область", "Кіровоградська", "Кировоградская область", "Кировоградская"),
        Region("hfu-region-luhanska", "Луганська область", "Луганська", "Луганская область", "Луганская"),
        Region("hfu-region-lvivska", "Львівська область", "Львівська", "Львовская область", "Львовская"),
        Region("hfu-region-mykolaivska", "Миколаївська область", "Миколаївська", "Николаевская область", "Николаевская"),
        Region("hfu-region-odeska", "Одеська область", "Одеська", "Одесская область", "Одесская"),
        Region("hfu-region-poltavska", "Полтавська область", "Полтавська", "Полтавская область", "Полтавская"),
        Region("hfu-region-rivnenska", "Рівненська область", "Рівненська", "Ровенская область", "Ровенская"),
        Region("hfu-region-sumska", "Сумська область", "Сумська", "Сумская область", "Сумская"),
        Region("hfu-region-ternopilska", "Тернопільська область", "Тернопільська", "Тернопольская область", "Тернопольская"),
        Region("hfu-region-kharkivska", "Харківська область", "Харківська", "Харьковская область", "Харьковская"),
        Region("hfu-region-khersonska", "Херсонська область", "Херсонська", "Херсонская область", "Херсонская"),
        Region("hfu-region-khmelnytska", "Хмельницька область", "Хмельницька", "Хмельницкая область", "Хмельницкая"),
        Region("hfu-region-cherkaska", "Черкаська область", "Черкаська", "Черкасская область", "Черкасская"),
        Region("hfu-region-chernivetska", "Чернівецька область", "Чернівецька", "Черновицкая область", "Черновицкая"),
        Region("hfu-region-chernihivska", "Чернігівська область", "Чернігівська", "Черниговская область", "Черниговская"),
        Region("hfu-region-kyiv-city", "м. Київ", "місто Київ", "м Київ", "город Киев", "г Киев"),
        Region("hfu-region-sevastopol-city", "м. Севастополь", "місто Севастополь", "м Севастополь", "город Севастополь", "г Севастополь")
    ];

    public IReadOnlyList<RegionReferenceItem> GetRegions()
    {
        return Regions;
    }

    private static RegionReferenceItem Region(
        string id,
        string name,
        params string[] aliases)
    {
        return new RegionReferenceItem(id, name, aliases);
    }
}
