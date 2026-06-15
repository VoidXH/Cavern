namespace Cavernize.Logic.Language;

/// <summary>
/// Strings used for generating a post-render report in Hungarian.
/// </summary>
public class RenderReportStringsHU : RenderReportStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "hu-HU";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("Defau", "Miután a renderelés befejeződött, itt további sávinformációt találsz, mint például a valódi objektumhasználati statisztika.");
        Set("ABeds", "Valójában tartalmazott bed csatornák");
        Set("AObjs", "Valójában tartalmazott dinamikus objektumok");
        Set("FakeT", "Kihasználatlan (kamu) renderelési célok");
        Set("PeaGa", "Csúcs audio keret szint");
        Set("RMSGa", "Tartalom RMS szintje");
        Set("MacDy", "Makrodinamika");
        Set("MicDy", "Mikrodinamika");
        Set("NoLFE", "Az LFE csatorna hiányzott a forrásból, kihasználatlan volt, vagy nem lett renderelve.");
        Set("PeaLF", "Csúcs LFE szint");
        Set("RMSLF", "RMS LFE szint");
        Set("MacLF", "LFE makrodinamika");
        Set("MicLF", "LFE mikrodinamika");
        Set("CheSl", "Mellbevágási osztályzat");
        Set("SurUs", "Surround használat");
        Set("HeiUs", "Magasság használat");
        Set("Grad0", "5*");
        Set("Grad1", "5");
        Set("Grad2", "4");
        Set("Grad3", "3");
        Set("Grad4", "2");
        Set("Grad5", "1");
    }
}
