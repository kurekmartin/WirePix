using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;

// Obecné informace o sestavení se řídí přes následující 
// sadu atributů. Změnou hodnot těchto atributů se upraví informace
// přidružené k sestavení.
[assembly: AssemblyTitle(title: "WirePix")]
[assembly: AssemblyDescription(description: "")]
[assembly: AssemblyConfiguration(configuration: "")]
[assembly: AssemblyCompany(company: "")]
[assembly: AssemblyProduct(product: "WirePix")]
[assembly: AssemblyCopyright(copyright: "Copyright ©  2020")]
[assembly: AssemblyTrademark(trademark: "")]
[assembly: AssemblyCulture(culture: "")]

// Nastavení ComVisible na false způsobí neviditelnost typů v tomto sestavení
// pro komponenty modelu COM. Pokud potřebujete přístup k typu v tomto sestavení
// z modelu COM, nastavte atribut ComVisible tohoto typu na True.
[assembly: ComVisible(visibility: false)]
[assembly: SupportedOSPlatform(platformName: "windows10.0.17763.0")]

//Pokud chcete začít vytvářet aplikace, které se dají lokalizovat, nastavte
//<UICulture>JazykováVerzeVeKteréPíšeteKód</UICulture> v souboru .csproj
//uvnitř <PropertyGroup>.  Pokud například používáte jazykovou verzi US english
//ve zdrojových souborech, nastavte <UICulture> na en-US.  Pak zrušte komentář
//pro atribut NeutralResourceLanguage.  Aktualizujte hodnotu "en-US" na
//dalším řádku, aby se shodovala s nastavením UICulture v souboru projektu.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    themeDictionaryLocation: ResourceDictionaryLocation.None, //kde se nacházejí zdrojové slovníky pro konkrétní motiv
    //(používá se, pokud se prostředek nenajde na stránce
    // nebo ve zdrojových slovnících aplikace)
    genericDictionaryLocation: ResourceDictionaryLocation.SourceAssembly //kde se nachází obecný zdrojový slovník
    //(používá se, pokud se prostředek nenajde na stránce
    // v aplikaci nebo libovolných zdrojových slovnících pro konkrétní motiv)
)]


// Informace o verzi sestavení se skládá z těchto čtyř hodnot:
//
//      Hlavní verze
//      Podverze
//      Číslo sestavení
//      Revize
//
// Můžete zadat všechny hodnoty nebo nastavit výchozí číslo buildu a revize
// pomocí zástupného znaku * takto:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(version: "0.5.1")]
[assembly: AssemblyFileVersion(version: "1.0.0")]