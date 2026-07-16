namespace CavernizeGUI.Language;

/// <summary>
/// Strings used in the main window UI in Hungarian.
/// </summary>
public class MainWindowStringsHU : MainWindowStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "hu-HU";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("MenuR", "Renderelés");
        Set("Upmix", "_Felkeverési beállítások...");
        Set("LoadV", "HRTF/HRIR szettek betöltése a _Virtualizálóba");
        Set("SpVir", "_Magasság virtualizálása hangszórókon");
        Set("FiltH", "Kimeneti _szűrők alkalmazása");
        Set("FiltT", "Betölt egy konvolúciós EQ szettet, amit a QuickEQ exportált a célrendszerhez, majd ezt használja a konvertált fájlba " +
            "beégetett szobakorrekcióként.");
        Set("MuBeH", "_Alap némítása");
        Set("MuBeT", "Csak azokat az objektumokat renderelje, amik valóban mozognak, nincs fix helyük.");
        Set("MuGrH", "_Talaj némítása");
        Set("MuGrT", "Minden hangot elnémít, ami nem a magasból jön.");
        Set("For24", "24-bites _PCM erőltetése (lassabb, nagyobb fájlok, nem ad hallható javulást)");
        Set("SuSwa", "_Oldalsó/hátsó kimeneti csatornák felcserélése");
        Set("WavCh", "RIFF WAVE csatornamaszk kihagyása (korlátozások feloldása)");
        Set("SMetH", "Metaadatok mutatása...");
        Set("SMetT", "Megmutatja a dekódolt kodekspecifikus mezőit a kiválasztott sávnak.");
        Set("ReMoH", "_Csak jelentés mód");
        Set("ReMoT", "Hogy ellenőrizd, hogy egy tartalom tényleg objektumalapú-e vagy sem, engedélyezd ezt az opciót, és a renderelés utáni jelentés " +
            "anélkül válik elérhetővé, hogy bármit írnál a lemezre.");
        Set("DeGrH", "Minőséganalízis és osztályozás");
        Set("DeGrT", "Érzékelhető hangminőségi metrikák mérése és a tartalom osztályozása. Ezt az információt a renderelés utáni jelentés fogja tartalmazni.");
        Set("PReSh", "_Renderelés utáni jelentés megnyitása...");
        Set("PReRe", "Renderelés utáni jelentés");
        Set("MenuH", "Súgó");
        Set("UsrGu", "_Felhasználói kézikönyv (angol)");
        Set("About", "_Névjegy");
        Set("MenuL", "Nyelv");
        Set("SySet", "Rendszer");
        Set("RSInf", "Válassz ki egy hangszórókiosztást, és helyezd el úgy a hangszóróid. Kattints a \"Bekötés mutatása\" gombra, hogy megtudd, melyik kimenetet " +
            "melyik valódi csatornára kell kötnöd. A maximális hangminőséghez kalibráld a rendszered QuickEQ-val.");
        Set("RndTg", "Render célja:");
        Set("DisWi", "Bekötés mutatása");
        Set("FFLoc", "FFmpeg keresése");
        Set("FFDes", "Keresd meg az FFmpeg-et ezzel a gombbal. Töltsd le az FFmpeg-et, majd keresd meg az ffmpeg.exe-t a letöltés bin mappájában. A Cavern az " +
            "FFmpeg-et használja az újrakódoláshoz. Ez az ideiglenes megoldás sok helyet használ, ami miatt legalább 10 GB extra szabad helyre lesz szükséged " +
            "egy 2 órás filmhez.");
        Set("CoPro", "Tartalom");
        Set("OpCnt", "Megnyitás");
        Set("OpTrk", "Hangsáv:");
        Set("OpOut", "Kimenet:");
        Set("OpRnd", "Renderelés");
        Set("ChkUp", "Frissítések automatikus keresése");
        Set("ChkTt", "Alkalmanként automatikusan megnézi, hogy áll-e rendelkezésre új frissítés.");
        Set("Queue", "Sor");
        Set("QuDes", "A konverziókat egymás után sorbaállíthatod ahelyett, hogy mindegyiket manuálisan hajtanád végre. Kattints a \"Sorhoz adás\" gombra, hogy egy " +
            "beállított konverziót sorba tégy, majd a \"Feldolgozás\" gombbal indíthatod el a sorba állított tartalmak konverzióját.");
        Set("QuAdd", "Sorhoz adás");
        Set("QuRem", "Kijelölt törlése");
        Set("QuSta", "Feldolgozás");
        Set("dnErr", "A .NET 6 telepítésed sérült és nem tölthető be. Kérlek, telepítsd a legújabbat a Microsoft-tól. Nyomj meg egy gombot a kilépéshez...");
        Set("DropF", "Több fájlt egyszerre csak a Soron lehet elhelyezni. Húzd ki az ablak jobb szélét, hogy megjelenítsd.");
        Set("IrErr", "Az impulzusválasz fájl hibás. Pontosabban: {0}");
        Set("ImFmt", "Minden támogatott formátum|{0}|Filmek|*.mkv;*.mka;*.mov;*.mp4;*.qt;*.webm;*.weba|(Enhanced) AC-3|*.ac3;*.eac3;*.ec3|Core Audio Format|*.caf|" +
            "Dolby Atmos Master Format|*.atmos|RIFF WAVE, ADM BWF|*.wav|Limitless Audio Format|*.laf");
        Set("OpRun", "Egy művelet már fut, kérlek várj, amíg befejeződik.");
        Set("OpRes", "A módosítások a Cavernize újraindítása után lépnek életbe.");
        Set("LdSrc", "Kérlek, tölts be egy fájlt, aminek legalább egy hangsávja támogatott rendereléshez.");
        Set("UnTrk", "A kiválasztott hangsáv nem támogatott rendereléshez.");
        Set("ChCnt", "A render céljának nagyobb a csatornaszáma ({0}), mint amit a kiválasztott formátum képes kezelni ({1}). Kérlek, válassz más formátumot vagy " +
            "kevesebb csatornás rendercélt.");
        Set("UnExt", "A fájlnév kiterjesztése nem támogatott.");
        Set("UnCod", "Ez a kodek nem támogatott exportáláshoz.");
        Set("Start", "Renderelés indítása...");
        Set("ExpOk", "Befejezve!");
        Set("Error", "Hiba");
        Set("FiltI", "Impulzusválasz csomagok|*.wav");
        Set("FiltF", "Cavern QuickEQ konvolúciós EQ-k|*.txt");
        Set("FiltC", "Ütköző mintavételezési frekvenciák: a fejhallgató virtualizáció és a kimeneti szűrők csak akkor működnek együtt, ha a mintavételezési " +
            "frekvenciájuk egyezik. Egyiküknek mennie kell.");
        Set("FFRea", "Készen áll!");
        Set("FFNRe", "Az FFmpeg nem található, kodekkorlátok vannak érvényben.");
        Set("ProgP", "Renderelés... ({0}, sebesség: {1}x, hátravan: {2})");
        Set("FinaP", "Véglegesítés... ({0})");
        Set("DropI", "Ezek a kihagyott fájlok nem támogatottak vagy sérültek:");
        Set("AbouH", "Névjegy");
        Set("AbouA", "A teljesítményt a CavernAmp gyorsítja.");
        Set("ReQOp", "Nem lehet feldolgozás alatt álló elemeket törölni a sorból.");
        Set("ReQSe", "Nincs kijelölt sorban álló elem.");
        Set("FFOnl", "A kiválasztott kimeneti formátumhoz FFmpeg-re van szükség. Kérlek, keresd meg az \"FFmpeg keresése\" gombbal, segítségért pedig kattints a " +
            "Súgó/Felhasználói kézikönyv gombra.");
        Set("CMetT", "Kodek metaadatok");
        Set("CMeET", "Légy szíves, előbb tölts be egy fájlt.");
        Set("CMeUT", "A Cavern API még nem támogatja a kiválasztott sáv metaadatainak megjelenítését.");
        Set("JocWa", "A fájl néhány részét ritka JOC-ban kódolták. Az E-AC-3 szabványnak ezen része nincs helyesen dokumentálva, emiatt rövid némítások " +
            "várhatók a hangban.");
        Set("RenEr", "A hang nem renderelhető a következő ponton: {0}. A feldolgozás folytatódik, de a sáv nem marad összefüggő ezután az idő után. " +
            "A pontos hiba: {1}");
        Set("SpViE", "A jelenlegi kiosztás nem támogatja magasság virtualizálását hangszórókon. Kapcsold ki ezt az opciót, vagy használj olyan kiosztást, " +
            "ahol minden csatorna a földön van.");
        Set("QuAlT", "Kombinált feldolgozás");
        Set("QuAll", "Szeretnél egy közös kimeneti mappát választani az összes fájlhoz, amit a sorhoz adsz? Ha az Igenre nyomsz, mindegyik az alapértelmezett " +
            "kimeneti konténerben lesz feldolgozva a kiválasztott mappába. Ha a Nemre nyomsz, egyesével kiválaszthatod a kimeneti mappákat, fájlneveket, " +
            "és konténerformátumokat.");
        Set("ExIEC", "Néhány csatornát ({0}) nem támogat az exportálási formátum. Megkerülheted ezt a korlátozást az --unsafe parancssori argumentummal, " +
            "de ez elronthatja a térbeli pontosságot.");
    }
}
