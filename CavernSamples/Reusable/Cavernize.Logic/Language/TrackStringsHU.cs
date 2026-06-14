namespace Cavernize.Logic.Language;

/// <summary>
/// Strings used in the track selection UI in Hungarian.
/// </summary>
public class TrackStringsHU : TrackStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "hu-HU";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("NoSup", "Cavern által nem támogatott hangsáv");
        Set("E3JOC", "Enhanced AC-3 közösített objektumokkal");
        Set("ObTra", "Objektumalapú hangsáv");
        Set("ChTra", "Csatornaalapú hangsáv");
        Set("SouCh", "Forrás csatornák");
        Set("MatBe", "Mátrixolt alap");
        Set("MatOb", "Mátrixolt obj.");
        Set("SouBe", "Alap csatornák");
        Set("SouDy", "Dinamikus obj.");
        Set("Chans", "Csatornák");
        Set("WiObj", "objektumokkal");
        Set("InvTr", "Ezt a hangsávot nem lehet dekódolni. A következő hiba történt a dekódolás során:");
        Set("Later", "Ez későbbi verziókban megjavulhat. Kérlek, próbáld meg frissíteni a Cavernize-t, " +
            "és ha a probléma továbbra is fennáll a legújabb verzióban is, írj a fejlesztőnek a www.sbence.hu-n.");
        Set("PCMFl", "PCM (lebegőpontos)");
        Set("PCMLE", "PCM (egész)");
        Set("C_AC3", "AC-3 (középszerű, támogat SPDIF-et)");
        Set("CEAC3", "Enhanced AC-3 (középszerű, támogat HDMI ARC-t)");
        Set("COpus", "Opus (transzparens, kis méret)");
        Set("CFLAC", "FLAC (veszteségmentes, nagy méret)");
        Set("CPCMF", "PCM, lebegőpontos (szükségtelen, legnagyobb méret)");
        Set("CPCMI", "PCM, egész (veszteségmentes, nagyobb méret)");
        Set("CADMC", "ADM Broadcast Wave Format (tömör)");
    }
}
