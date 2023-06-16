"use strict";

let mydb;

class Mod {
    async postDBLoadAsync(container) {
        const db = container.resolve("DatabaseServer").getTables();
        const modLoader = container.resolve("PreAkiModLoader");
        const importerUtil = container.resolve("ImporterUtil");
        const locales = db.locales.global;
        const items = db.templates.items;
        const handbook = db.templates.handbook.Items;
        const quests = db.templates.quests;

        mydb = await importerUtil.loadRecursiveAsync(`${modLoader.getModPath("DoorBreacher")}database/`);

        for (const item in mydb.templates.items) {
            items[item] = mydb.templates.items[item];
        }

        for (const item of mydb.templates.handbook.Items) {
            handbook.push(item);
        }


        for (const item of mydb.traders.assort.assorts.items) {
            db.traders[mydb.traders.assort.traderId].assort.items.push(item);
        }

        for (const bc in mydb.traders.assort.assorts.barter_scheme) {
            db.traders[mydb.traders.assort.traderId].assort.barter_scheme[bc] = mydb.traders.assort.assorts.barter_scheme[bc];
        }

        for (const level in mydb.traders.assort.assorts.loyal_level_items) {
            db.traders[mydb.traders.assort.traderId].assort.loyal_level_items[level] = mydb.traders.assort.assorts.loyal_level_items[level];
        }

        for (const localeID in locales) {
                for (const [itemId, template] of Object.entries(mydb.locales.en.templates)) {
                    for (const [key, value] of Object.entries(template)) {
                        locales[localeID][`${itemId} ${key}`] = value;
                    }
                }
            }
        


        Mod.addAmmoToMags(db);
		 Mod.addAmmoToMags1(db);
		  Mod.addAmmoToMags2(db);
		    Mod.addAmmoToMags3(db);
    }

    static addAmmoToMags(db) {
        const isModFilterExist = (Cartridges) => Cartridges.findIndex((cartridge) => cartridge._name === "cartridges");
        const isItemSlotsExist = (item) => item._props.Cartridges && item._props.Cartridges.length > 0;
        const filtersIncludeAttachment = (filterArray) => filterArray.includes("560d5e524bdc2d25448b4571");
        for (const item of Object.values(db.templates.items)) {
            if (isItemSlotsExist(item)) {
                const index = isModFilterExist(item._props.Cartridges);
                if (index > -1 && filtersIncludeAttachment(item._props.Cartridges[index]._props.filters[0].Filter)) {
                    item._props.Cartridges[index]._props.filters[0].Filter.push("doorbreacher");
                }
            }
        }
    }
 static addAmmoToMags1(db) {
        const isModFilterExist = (Chambers) => Chambers.findIndex((chamber) => chamber._name === "patron_in_weapon");
        const isItemSlotsExist = (item) => item._props.Chambers && item._props.Chambers.length > 0;
        const filtersIncludeAttachment = (filterArray) => filterArray.includes("560d5e524bdc2d25448b4571");
        for (const item of Object.values(db.templates.items)) {
            if (isItemSlotsExist(item)) {
                const index = isModFilterExist(item._props.Chambers);
                if (index > -1 && filtersIncludeAttachment(item._props.Chambers[index]._props.filters[0].Filter)) {
                    item._props.Chambers[index]._props.filters[0].Filter.push("doorbreacher");
                }
            }
        }
    }
static addAmmoToMags2(db) {
        const isModFilterExist = (Chambers) => Chambers.findIndex((chamber) => chamber._name === "patron_in_weapon_000");
        const isItemSlotsExist = (item) => item._props.Chambers && item._props.Chambers.length > 0;
        const filtersIncludeAttachment = (filterArray) => filterArray.includes("560d5e524bdc2d25448b4571");
        for (const item of Object.values(db.templates.items)) {
            if (isItemSlotsExist(item)) {
                const index = isModFilterExist(item._props.Chambers);
                if (index > -1 && filtersIncludeAttachment(item._props.Chambers[index]._props.filters[0].Filter)) {
                    item._props.Chambers[index]._props.filters[0].Filter.push("doorbreacher");
                }
            }
        }
    }
		static addAmmoToMags3(db) {
        const isModFilterExist = (Chambers) => Chambers.findIndex((chamber) => chamber._name === "patron_in_weapon_001");
        const isItemSlotsExist = (item) => item._props.Chambers && item._props.Chambers.length > 0;
        const filtersIncludeAttachment = (filterArray) => filterArray.includes("560d5e524bdc2d25448b4571");
        for (const item of Object.values(db.templates.items)) {
            if (isItemSlotsExist(item)) {
                const index = isModFilterExist(item._props.Chambers);
                if (index > -1 && filtersIncludeAttachment(item._props.Chambers[index]._props.filters[0].Filter)) {
                    item._props.Chambers[index]._props.filters[0].Filter.push("doorbreacher");
                }
            }
        }
    }
   
        
    }


module.exports = { mod: new Mod() }