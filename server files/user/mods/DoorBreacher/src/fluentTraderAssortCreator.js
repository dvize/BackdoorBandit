"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.FluentAssortConstructor = void 0;
class FluentAssortConstructor {
    itemsToSell = [];
    barterScheme = {};
    loyaltyLevels = {};
    hashUtil;
    logger;
    constructor(hashUtil, logger) {
        this.hashUtil = hashUtil;
        this.logger = logger;
    }
    createSingleAssortItem(itemTpl, itemId = undefined) {
        const newItem = {
            _id: itemId || this.hashUtil.generate(),
            _tpl: itemTpl,
            parentId: "hideout",
            slotId: "hideout",
            upd: { UnlimitedCount: false, StackObjectsCount: 100 }
        };
        this.itemsToSell.push(newItem);
        return this;
    }
    addStackCount(stackCount, itemId) {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            item.upd.StackObjectsCount = stackCount;
        }
        return this;
    }
    addUnlimitedStackCount(itemId) {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            item.upd.StackObjectsCount = 999999;
            item.upd.UnlimitedCount = true;
        }
        return this;
    }
    addBuyRestriction(maxBuyLimit, itemId) {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            item.upd.BuyRestrictionMax = maxBuyLimit;
            item.upd.BuyRestrictionCurrent = 0;
        }
        return this;
    }
    addLoyaltyLevel(level, itemId) {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            this.loyaltyLevels[item._id] = level;
        }
        return this;
    }
    addMoneyCost(currencyType, amount, itemId) {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            this.barterScheme[item._id] = [[{ count: amount, _tpl: currencyType }]];
        }
        return this;
    }
    addBarterCost(itemTpl, count, itemId) {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (!this.barterScheme[item._id]) {
            this.barterScheme[item._id] = [[{ count, _tpl: itemTpl }]];
        }
        else {
            const scheme = this.barterScheme[item._id][0].find(s => s._tpl === itemTpl);
            if (scheme) {
                scheme.count += count;
            }
            else {
                this.barterScheme[item._id][0].push({ count, _tpl: itemTpl });
            }
        }
        return this;
    }
    export(data) {
        for (const item of this.itemsToSell) {
            if (data.assort.items.some(i => i._id === item._id)) {
                this.logger.error(`Item with ID ${item._id} already exists in the assortment.`);
                return;
            }
            //this.logger.info(`Adding item with ID ${item._id} to the assortment.`)
            //this.logger.info(item);
            data.assort.items.push(item);
            if (this.barterScheme[item._id]) {
                data.assort.barter_scheme[item._id] = this.barterScheme[item._id];
            }
            if (this.loyaltyLevels[item._id]) {
                data.assort.loyal_level_items[item._id] = this.loyaltyLevels[item._id];
            }
        }
        // Reset internal state
        this.itemsToSell = [];
        this.barterScheme = {};
        this.loyaltyLevels = {};
        return this;
    }
}
exports.FluentAssortConstructor = FluentAssortConstructor;
//# sourceMappingURL=fluentTraderAssortCreator.js.map