"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const Money_1 = require("C:/snapshot/project/obj/models/enums/Money");
const fluentTraderAssortCreator_1 = require("./fluentTraderAssortCreator");
const node_path_1 = __importDefault(require("node:path"));
class Mod {
    // Declare private variable db of DatabaseServer type
    dbServer;
    db;
    fluentTraderAssortHelper;
    traderID;
    itemsJson;
    logger;
    jsonUtil;
    postDBLoad(container) {
        // Resolve containers
        this.logger = container.resolve("WinstonLogger");
        const customItem = container.resolve("CustomItemService");
        const hashUtil = container.resolve("HashUtil");
        this.dbServer = container.resolve("DatabaseServer");
        this.db = this.dbServer.getTables();
        this.fluentTraderAssortHelper = new fluentTraderAssortCreator_1.FluentAssortConstructor(hashUtil, this.logger);
        this.jsonUtil = container.resolve("JsonUtil");
        // Get fileSystem to read in configs
        const fileSystem = container.resolve("FileSystemSync");
        const itemsJsonPath = node_path_1.default.resolve(__dirname, "../database/templates/items.jsonc");
        // Read the items.json file with type ItemsJson
        this.itemsJson = this.jsonUtil.deserializeJsonC(fileSystem.read(itemsJsonPath));
        // Set trader id we want to add assort items to
        this.traderID = "5a7c2eca46aef81a7ca2145d";
        // Load hideoutrecipes and our custom recipes
        const hideoutRecipes = this.db.hideout.production.recipes;
        const customRecipes = this.jsonUtil.deserializeJsonC(fileSystem.read(node_path_1.default.resolve(__dirname, "../database/templates/craftingItem.jsonc")));
        setupItems(this.itemsJson, customItem);
        handleAssorts(this.db, this.fluentTraderAssortHelper, this.traderID, this.itemsJson);
        modifyAmmoPropForWeapons(this.db, this.itemsJson);
        this.logger.info("DoorBreacher: Finished Modifying Ammo Properties for Weapons");
        this.logger.info(`Before ${hideoutRecipes.length}`);
        // Adds custom recipe(s) to the workbench. Currently only the C4
        customRecipes.forEach(customRecipe => {
            hideoutRecipes.push(customRecipe);
        });
        this.logger.info(`After ${hideoutRecipes.length}`);
        this.logger.info(`Is recipe loaded? ${hideoutRecipes.filter(r => r._id === "665d4ce7e381d16c8676292b").length >= 1}`);
        this.logger.info("Added custom recipes to hideout recipes");
    }
}
function setupItems(itemsjson, customItem) {
    //Make locale for DoorBreacher
    const doorBreacherLocale = {
        en: {
            name: "12/70 Door-Breaching Round",
            shortName: "Breach",
            description: "The door-breaching round is designed to destroy deadbolts, locks, and hinges without risking lives by ricocheting or penetrating through doors. These frangible rounds are made of a dense sintered material which can destroy a lock or hinge and then immediately disperse."
        }
    };
    // Add new custom item
    const doorBreacher = {
        newItem: itemsjson.doorbreacher,
        fleaPriceRoubles: 8000,
        handbookPriceRoubles: 10000,
        handbookParentId: "5b47574386f77428ca22b33b",
        locales: doorBreacherLocale
    };
    // Make locale for DoorBreacherBox
    const doorBreacherBoxLocale = {
        en: {
            name: "12/70 Door-Breaching 5-Round Box",
            shortName: "Breach",
            description: "A 5-round box of 12ga door breaching shells. The door-breaching round is designed to destroy deadbolts, locks, and hinges without risking lives by ricocheting or penetrating through doors.  These frangible rounds are made of a dense sintered material which can destroy a lock or hinge and then immediately disperse."
        }
    };
    // Add new custom item
    const doorBreacherBox = {
        newItem: itemsjson.doorbreacherbox,
        fleaPriceRoubles: 40000,
        handbookPriceRoubles: 50000,
        handbookParentId: "5b47574386f77428ca22b33c",
        locales: doorBreacherBoxLocale
    };
    // Make locale for DoorBreacher
    const c4ExplosiveLocale = {
        en: {
            name: "C4 Explosive",
            shortName: "C4",
            description: "This C4 Explosive is used for breaching reinforced doors. It is a powerful explosive that is used in the military and law enforcement. It is a plastic explosive that is stable and safe to handle and triggered after a set timer."
        }
    };
    // Add new custom item
    const c4Explosive = {
        newItem: itemsjson.C4Explosive,
        fleaPriceRoubles: 45000,
        handbookPriceRoubles: 40000,
        handbookParentId: "5b47574386f77428ca22b2f2",
        locales: c4ExplosiveLocale
    };
    // Create the items
    customItem.createItem(doorBreacher);
    customItem.createItem(doorBreacherBox);
    customItem.createItem(c4Explosive);
}
function modifyAmmoPropForWeapons(db, itemsJson) {
    const weaponProperties = [
        { name: "Chambers", index: 0 },
        { name: "Cartridges", index: 1 },
        { name: "camora_000", index: 2 },
        { name: "camora_001", index: 3 },
        { name: "camora_002", index: 4 },
        { name: "camora_003", index: 5 },
        { name: "camora_004", index: 6 }
    ];
    const is12GaugeAmmo = (filters) => {
        return filters ? filters.some(filter => filter.Filter?.includes("560d5e524bdc2d25448b4571")) : false;
    };
    const addDoorBreacher = (item, filters, weaponPropName) => {
        console.info(`DoorBreacher added to: ${item._name} in weaponPropName: ${weaponPropName}`);
        filters[0].Filter.push(itemsJson.doorbreacher._id.toString());
    };
    const processWeaponProperty = (item, weaponPropName) => {
        const property = item._props[weaponPropName];
        if (!property) {
            return;
        }
        if (Array.isArray(property)) {
            // For properties like "Chambers"
            for (const subProperty of property) {
                if (subProperty._props.filters && is12GaugeAmmo(subProperty._props.filters)) {
                    addDoorBreacher(item, subProperty._props.filters, weaponPropName);
                }
            }
        }
        else {
            // For properties directly under _props like "Cartridges"
            if (property.filters && is12GaugeAmmo(property.filters)) {
                addDoorBreacher(item, property.filters, weaponPropName);
            }
        }
    };
    const processSlots = (slots) => {
        if (!slots || slots.length === 0) {
            return;
        }
        for (const slot of slots) {
            if (slot._props.filters && is12GaugeAmmo(slot._props.filters)) {
                addDoorBreacher(slot, slot._props.filters, slot._name);
            }
        }
    };
    // Iterate over all items
    for (const item of Object.values(db.templates.items)) {
        for (const prop of weaponProperties) {
            if (item._props[prop.name]) {
                processWeaponProperty(item, prop.name);
            }
        }
        // Process slots for "camora"
        if (item._props.Slots) {
            processSlots(item._props.Slots);
        }
    }
}
function handleAssorts(db, assortHelper, traderID, itemsjson) {
    const targetTrader = db.traders[traderID];
    // Create assort for doorbreacher. No money, add barter only later
    assortHelper
        .createSingleAssortItem(itemsjson.doorbreacher._id)
        .addStackCount(100)
        .addUnlimitedStackCount()
        .addLoyaltyLevel(1)
        .addMoneyCost(Money_1.Money.ROUBLES, 10000)
        .export(targetTrader);
    // Create assort for doorbreacherbox - no assort since no other trader sells a packl
    // assortHelper
    //   .createSingleAssortItem(itemsjson.doorbreacherbox._id)
    //   .addStackCount(100)
    //   .addUnlimitedStackCount()
    //   .addLoyaltyLevel(1)
    //   .addMoneyCost(Money.ROUBLES, 50000)
    //   .export(targetTrader);
    // Create barter item for doorbreacher
    const electricWire = "5c06779c86f77426e00dd782";
    assortHelper
        .createSingleAssortItem(itemsjson.doorbreacher._id)
        .addStackCount(100)
        .addUnlimitedStackCount()
        .addBarterCost(electricWire, 1)
        .addLoyaltyLevel(1)
        .export(targetTrader);
}
module.exports = { mod: new Mod() };
//# sourceMappingURL=mod.js.map