using System;
using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public sealed class WardrobePreset { public string name; public WardrobeLoadout loadout = new WardrobeLoadout(); }
    [Serializable] sealed class WardrobePresetStore { public int active; public WardrobePreset[] slots; }
    // Each slot is an editable loadout. The wardrobe page edits the slot being
    // viewed; only the active slot is mirrored into WardrobeSelection (what is
    // actually worn). New slots are blank: a category left empty means the
    // default item. Only catalog identifiers are stored.
    public static class WardrobePresets {
        const string Key = "riichi.wardrobe.presets.v3";
        public const int Count = 10;
        static WardrobePresetStore store;
        static void Load() {
            if (store != null) return;
            try { store = JsonUtility.FromJson<WardrobePresetStore>(PlayerPrefs.GetString(Key, "{}")); } catch { store = null; }
            if (store == null) store = new WardrobePresetStore();
            var slots = new WardrobePreset[Count];
            for (int i = 0; i < Count; i++) {
                var old = store.slots != null && i < store.slots.Length ? store.slots[i] : null;
                slots[i] = old ?? new WardrobePreset();
                if (string.IsNullOrEmpty(slots[i].name)) slots[i].name = "预设" + (i + 1);
                if (slots[i].loadout == null) slots[i].loadout = new WardrobeLoadout();
            }
            store.slots = slots;
            if (store.active < 0 || store.active >= Count) store.active = 0;
        }
        static void Persist() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(store)); PlayerPrefs.Save(); }
        public static int Active { get { Load(); return store.active; } }
        public static string Name(int i) { Load(); return store.slots[Mathf.Clamp(i, 0, Count - 1)].name; }
        // Empty string or null: the category is not set in this slot (default item).
        public static string Id(int i, string category) { Load(); return store.slots[Mathf.Clamp(i, 0, Count - 1)].loadout.Id(category); }
        // The worn selection can change outside the wardrobe (missing packs fall
        // back), so the active slot follows it whenever the wardrobe opens.
        // Categories still at their default stay blank.
        public static void SyncActive() {
            Load(); var slot = store.slots[store.active].loadout;
            foreach (var f in typeof(WardrobeLoadout).GetFields()) {
                var worn = WardrobeSelection.Get(f.Name)?.id; var set = f.GetValue(slot) as string;
                if (string.IsNullOrEmpty(set) && worn == WardrobePackService.Fallback(f.Name)?.id) continue;
                f.SetValue(slot, worn);
            }
            Persist();
        }
        public static void Set(int i, string category, string id) {
            Load(); if (i < 0 || i >= Count) return;
            var field = typeof(WardrobeLoadout).GetField(category); if (field == null) return;
            field.SetValue(store.slots[i].loadout, id); Persist();
            if (i == store.active) WardrobeSelection.Save(new[] { new KeyValuePair<string, string>(category, id) });
        }
        // Wears slot i; empty categories wear the default item. Returns how many
        // categories kept their current item because the slot's item is not installed.
        public static int Use(int i) {
            Load(); if (i < 0 || i >= Count) return 0;
            var choices = new List<KeyValuePair<string, string>>(); int skipped = 0;
            foreach (var field in typeof(WardrobeLoadout).GetFields()) {
                var id = field.GetValue(store.slots[i].loadout) as string;
                var item = string.IsNullOrEmpty(id) ? WardrobePackService.Fallback(field.Name) : WardrobeCatalog.Find(field.Name == "hand" ? WardrobeCatalog.CanonicalHand(id) : id);
                if (item == null || item.category != field.Name) continue;
                if (!WardrobePackService.Installed(item.id)) { skipped++; continue; }
                choices.Add(new KeyValuePair<string, string>(field.Name, item.id));
            }
            if (choices.Count > 0) WardrobeSelection.Save(choices);
            store.active = i; Persist();
            return skipped;
        }
    }
}
