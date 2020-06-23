import {RootStore} from "./rootStore";
import {action, computed, observable, runInAction} from "mobx";
import {ISlipway} from "../models/slipway";
import {toast} from "react-toastify";
import agent from "../api/agent";

export default class SlipwayStore {
    rootStore: RootStore;

    constructor(rootStore: RootStore) {
        this.rootStore = rootStore;
    }

    @observable slipwayRegistry = new Map();
    @observable slipway: ISlipway | null = null;
    @observable loadingSlipways = false;
    @observable search = "";

    @computed get slipways(): ISlipway[] {
        return Array.from(this.slipwayRegistry.values());
    };

    @action setSearch = (value: string) => {
        console.log(value);
        runInAction('setSearch', () => {
            this.search = value;
            console.log(this.search);
        });
    };

    @action reset = () => {
        this.search = "";
        this.slipwayRegistry.clear();
    }

    @action loadSlipways = async (value: string = "*") => {
        this.loadingSlipways = true;
        const slipways = await agent.Slipways.list();
        runInAction('loading slipways', () => {
            this.slipwayRegistry.clear();
            value = value.toLowerCase();
            slipways.forEach(slipway => {
                //activity.date = new Date(activity.date);
                if(slipway.name.toLowerCase().includes(value) || slipway.city.toLowerCase().includes(value) || slipway.water.toLowerCase().includes(value) ||  value === '*')
                {
                    this.slipwayRegistry.set(slipway.id, slipway);
                }
            });
            this.loadingSlipways = false;
        });
        try {
        } catch (error) {
            runInAction('load slipways error', () => {
                toast.error(error);
                throw error;
            });
            this.loadingSlipways = false;
        }
    };

    @action loadSlipway = async (id: string) => {
        let slipway = this.slipwayRegistry.get(id);
        if (slipway) {
            this.slipway = slipway;
            return slipway;
        } else {
            this.loadingSlipways = true;
            try {
                slipway = await agent.Slipways.details(id);
                runInAction('loading slipway', () => {
                    this.slipway = slipway;
                    this.slipwayRegistry.set(slipway.id, slipway);
                    this.loadingSlipways = false;
                })
                return slipway;
            } catch (error) {
                runInAction('error loading slipway', () => {
                    this.loadingSlipways = false;
                    toast.error(error);
                    throw error;
                })
            }
        }
    };
}
