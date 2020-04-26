import {RootStore} from "./rootStore";
import {action, computed, observable, runInAction} from "mobx";
import {IWater} from "../models/water";
import {toast} from "react-toastify";
import agent from "../api/agent";

export default class WaterStore{
    rootStore: RootStore;
    constructor(rootStore: RootStore) {
        this.rootStore = rootStore;
    }

    @observable waterRegistry = new Map();
    @observable loadingWater = false;
    @observable water: IWater | null = null;

    @computed get waters(){
        return Array.from(this.waterRegistry.values());
    }

    @action loadWaters = async () => {
        this.loadingWater = true;
        try{
            const waters = await agent.Waters.list();
            runInAction("load waters", () =>{
                waters.forEach(water => {
                    this.waterRegistry.set(water.id, water);
                });
                this.loadingWater = false;
            })
        }catch(error){
            runInAction("loading waters error", () =>{
                this.loadingWater = false;
                toast.error(error);
                throw error;
            })
        }
    }
}
