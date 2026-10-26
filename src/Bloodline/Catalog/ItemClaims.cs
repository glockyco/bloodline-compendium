using Bloodline.Code;
using Bloodline.Extraction;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal enum ClaimType
{
    Extracted,
    Formula,
    Observed,
    Unknown,
}

internal sealed record CodeCitation(string Function, string Fingerprint);

internal sealed record FactClaim(
    string Item,
    string Fact,
    ClaimType Type,
    long Build,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<CodeCitation> Citations
);

internal sealed record ClaimReport(long Build, IReadOnlyList<FactClaim> Facts);

internal static class ClaimCitations
{
    public static IReadOnlyList<CodeCitation> Resolve(
        string fact,
        IEnumerable<string> citations,
        CodeIndex index
    ) =>
        Resolve(
            fact,
            citations,
            index.Functions.ToDictionary(function => function.Citation, StringComparer.Ordinal)
        );

    public static IReadOnlyList<CodeCitation> Resolve(
        string fact,
        IEnumerable<string> citations,
        IReadOnlyDictionary<string, CodeFunction> functions
    )
    {
        return citations
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(citation =>
            {
                if (!functions.TryGetValue(citation, out var function))
                    throw new InvalidDataException(
                        $"Fact {fact} cites missing function {citation}."
                    );
                if (function.ParseStatus != CodeParseStatus.Complete)
                    throw new InvalidDataException(
                        $"Fact {fact} cites partly parsed function {citation}: {string.Join(", ", function.Errors)}."
                    );
                return new CodeCitation(citation, function.Fingerprint);
            })
            .ToArray();
    }
}

internal sealed class ItemClaims(CodeIndex? code, long build)
{
    private readonly List<FactClaim> claims = [];
    private readonly Dictionary<string, IReadOnlyList<CodeCitation>> evidence = new(
        StringComparer.Ordinal
    );
    private readonly IReadOnlyDictionary<string, CodeFunction> functions = (
        code?.Functions ?? []
    ).ToDictionary(function => function.Citation, StringComparer.Ordinal);
    public IReadOnlyList<FactClaim> Claims => claims;

    public void Record(string item, ItemPage page)
    {
        Add("Name", page.Name, ["ItemInfo.ItemName"]);
        Add("Description", page.Description, ["ItemInfo.Description"]);
        var categoryInputs =
            page.Category.Name == ItemCategories.WeaponPart.Name
                ? new[] { "WeaponComponentTable.Class" }
            : page.Category.Name == ItemCategories.Currency.Name ? ["Class"]
            : ["ItemInfo.ItemType"];
        if (!page.Facts.Any(fact => fact.Stat == Stat.Category))
            Add(
                "Category",
                page.Category.Name == "Unknown" ? null : page.Category.Name,
                categoryInputs
            );
        Add(
            "Type",
            page.Type == "Unknown" ? null : page.Type,
            page.Category.Name switch
            {
                "Weapon" => ["ItemInfo.SpecificItemType"],
                "Armor" => ["ItemInfo.ItemSlot"],
                "Weapon Part" => ["WeaponComponentTable.Part"],
                _ => categoryInputs,
            }
        );
        foreach (var fact in page.Facts)
        {
            var inputs = Inputs(fact);
            Add(fact.Label, StatVocabulary.Display(fact), inputs);
        }

        void Add(string fact, string? value, string[] inputs)
        {
            var known = value is not null;
            var key = string.Join("|", inputs);
            if (!evidence.TryGetValue(key, out var citations))
            {
                citations = code is null
                    ? []
                    : ClaimCitations.Resolve(
                        item + ":" + fact,
                        inputs.SelectMany(input =>
                            code.References.Where(reference => Matches(reference.Target, input))
                                .SelectMany(reference => reference.Readers)
                                .Where(citation => Consumer(input, citation))
                        ),
                        functions
                    );
                evidence[key] = citations;
            }
            if (
                known
                && code is not null
                && citations.Count == 0
                && inputs.Any(input =>
                    input
                        is "WeaponInfo.Damage"
                            or "WeaponInfo.AttackSpeed"
                            or "ArmorInfo.DamageRating"
                    || input.StartsWith("WeaponComponentTable.", StringComparison.Ordinal)
                )
            )
                throw new InvalidDataException(
                    $"Fact {item}:{fact} has no consuming function for {string.Join(", ", inputs)}."
                );
            claims.Add(
                new FactClaim(
                    item,
                    fact,
                    known ? ClaimType.Extracted : ClaimType.Unknown,
                    build,
                    inputs,
                    citations
                )
            );
        }
    }

    private static bool Matches(CodeTarget target, string input)
    {
        var parts = input.Split('.', 2);
        if (parts.Length != 2)
            return false;
        var owner = target.Path.Split(':')[0].Split('/')[^1].Split('.')[0];
        if (target.Kind == CodeTargetKind.DataTable)
            return owner == parts[0];
        if (
            target.Kind is not (CodeTargetKind.StructField or CodeTargetKind.Property)
            || !target.Path.Contains(':')
        )
            return false;
        var field = FieldNames.StripSuffix(target.Path.Split(':')[^1].Split('.')[0]);
        var type = parts[0] switch
        {
            "ItemInfo" => "F_ItemInfoo",
            "WeaponInfo" => "F_WeaponINfo",
            "ArmorInfo" => "F_ArmorInfo",
            _ => parts[0],
        };
        return owner == type && field == parts[1];
    }

    private static bool Consumer(string input, string citation)
    {
        var package = citation.Split(':')[0];
        var player =
            package == "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2";
        var tooltip =
            package
            is "/Game/BloodlinesAlpha/UI/Inventory/UI_InventoryItem_2"
                or "/Game/BloodlinesAlpha/UI/CharacterSheet/UI_EquipmentSlot"
                or "/Game/fantasy_gui_4/widgets/sample_windows/Trader_Inventory_Item";
        // Cite the shared player and item consumers, not every skill that copies their fields.
        return input.Split('.')[0] switch
        {
            "ItemInfo" => tooltip || player,
            "WeaponInfo" => player
                || package == "/Game/BloodlinesAlpha/Interactables/Weapons/BP_BaseWeapon"
                || input == "WeaponInfo.DamageType"
                    && package == "/Game/Animations/FirstPersonCharacterBP_Modular1",
            "ArmorInfo" => player,
            "PotionInfo" => tooltip || player,
            "ReagentInfo" => package == "/Game/fantasy_gui_4/widgets/sample_windows/PotionCrafting",
            "WeaponComponentTable" => package == "/Game/BloodlinesAlpha/Crafting",
            _ => false,
        };
    }

    private static string[] Inputs(Fact fact) =>
        fact.Stat switch
        {
            Stat.Value => ["ItemInfo.ItemValue"],
            Stat.Category => ["ItemInfo.ItemType"],
            Stat.DamageContribution => ["WeaponComponentTable.Damage"],
            Stat.SpeedContribution => ["WeaponComponentTable.Speed"],
            Stat.WeaponType => ["WeaponInfo.AttackType"],
            Stat.Damage => ["WeaponInfo.Damage"],
            Stat.AttackSpeed => ["WeaponInfo.AttackSpeed"],
            Stat.Range => ["WeaponInfo.Range"],
            Stat.DamageType => ["WeaponInfo.DamageType"],
            Stat.MagazineSize => ["WeaponInfo.MagazineSize"],
            Stat.ArmorType => ["ItemInfo.ArmorType"],
            Stat.ArmorRating => ["ArmorInfo.DamageRating"],
            Stat.Effects => ["PotionInfo.PotionEffects"],
            Stat.Effect =>
            [
                "PotionInfo.PotionEffects",
                "PotionInfo.Effect" + fact.Label.Split(' ')[^1] + "Power",
                "PotionInfo.DurationofEffect" + fact.Label.Split(' ')[^1],
            ],
            Stat.RestorationAmount => ["PotionInfo.AmountofHealthorManatorestore"],
            Stat.HealthDamage => ["PotionInfo.Amountofdamagetodealtohealth"],
            Stat.ManaDamage => ["PotionInfo.Amountofdamagetodealtomana"],
            Stat.ChoppedEffect => Reagent("Chopped"),
            Stat.CooledEffect => Reagent("Cooled"),
            Stat.GroundEffect => Reagent("Ground"),
            Stat.HeatedEffect => Reagent("Heated"),
            _ => throw new InvalidOperationException("No claim inputs for " + fact.Stat),
        };

    private static string[] Reagent(string treatment) =>
        [
            "ReagentInfo." + treatment + "Effects",
            "ReagentInfo." + treatment + "EffectPower",
            "ReagentInfo." + treatment + "EffectDuration",
        ];
}
