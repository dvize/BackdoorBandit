import { IItem } from "@spt/models/eft/common/tables/IItem";
import { IBarterScheme, ITrader } from "@spt/models/eft/common/tables/ITrader";
import { Money } from "@spt/models/enums/Money";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { HashUtil } from "@spt/utils/HashUtil";

export class FluentAssortConstructor {
    protected itemsToSell: IItem[] = [];
    protected barterScheme: Record<string, IBarterScheme[][]> = {};
    protected loyaltyLevels: Record<string, number> = {};
    protected hashUtil: HashUtil;
    protected logger: ILogger;

    constructor(hashUtil: HashUtil, logger: ILogger) {
        this.hashUtil = hashUtil;
        this.logger = logger;
    }

    public createSingleAssortItem(itemTpl: string, itemId = undefined): FluentAssortConstructor {
        const newItem: IItem = {
            _id: itemId || this.hashUtil.generate(),
            _tpl: itemTpl,
            parentId: "hideout",
            slotId: "hideout",
            upd: { UnlimitedCount: false, StackObjectsCount: 100 }
        };
        this.itemsToSell.push(newItem);
        return this;
    }

    public addStackCount(stackCount: number, itemId?: string): FluentAssortConstructor {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            item.upd.StackObjectsCount = stackCount;
        }
        return this;
    }

    public addUnlimitedStackCount(itemId?: string): FluentAssortConstructor {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            item.upd.StackObjectsCount = 999999;
            item.upd.UnlimitedCount = true;
        }
        return this;
    }

    public addBuyRestriction(maxBuyLimit: number, itemId?: string): FluentAssortConstructor {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            item.upd.BuyRestrictionMax = maxBuyLimit;
            item.upd.BuyRestrictionCurrent = 0;
        }
        return this;
    }

    public addLoyaltyLevel(level: number, itemId?: string): FluentAssortConstructor {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            this.loyaltyLevels[item._id] = level;
        }
        return this;
    }

    public addMoneyCost(currencyType: Money, amount: number, itemId?: string): FluentAssortConstructor {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (item) {
            this.barterScheme[item._id] = [[{ count: amount, _tpl: currencyType }]];
        }
        return this;
    }

    public addBarterCost(itemTpl: string, count: number, itemId?: string): FluentAssortConstructor {
        const item = itemId ? this.itemsToSell.find(i => i._id === itemId) : this.itemsToSell[0];
        if (!this.barterScheme[item._id]) {
            this.barterScheme[item._id] = [[{ count, _tpl: itemTpl }]];
        } else {
            const scheme = this.barterScheme[item._id][0].find(s => s._tpl === itemTpl);
            if (scheme) {
                scheme.count += count;
            } else {
                this.barterScheme[item._id][0].push({ count, _tpl: itemTpl });
            }
        }
        return this;
    }

    public export(data: ITrader): FluentAssortConstructor {
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
