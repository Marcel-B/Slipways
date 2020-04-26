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

    @computed get slipways(): ISlipway[] {
        return Array.from(this.slipwayRegistry.values());
    };

    @action loadSlipways = async () => {
        this.loadingSlipways = true;
        const slipways = await agent.Slipways.list();
        runInAction('loading slipways', () => {
            slipways.forEach(slipway => {
                //activity.date = new Date(activity.date);
                this.slipwayRegistry.set(slipway.id, slipway);
            });
            this.loadingSlipways = false;
        });
        try{

        }catch(error){
            runInAction('load slipways error', () => {
                toast.error(error);
                throw error;
            });
            this.loadingSlipways = false;
        }
    };

    @action loadSlipway = async (id: string) => {

        let slipway = this.slipwayRegistry.get(id);
        if(slipway){
            this.slipway = slipway;
            return slipway;
        }else {
            this.loadingSlipways = true;
            try{
                slipway = await agent.Slipways.details(id);
                runInAction('loading slipway', () => {
                    this.slipway = slipway;
                    this.slipwayRegistry.set(slipway.id, slipway);
                    this.loadingSlipways = false;
                })
                return slipway;
            }catch(error){
                runInAction('error loading slipway', ()=>{
                    this.loadingSlipways = false;
                    toast.error(error);
                    throw error;
                })
            }
        }
    };
}
