import {configure} from "mobx";
import {createContext} from "react";
import CommonStore from "./commonStore";
import UserStore from "./userStore";
import ModalStore from "./modalStore";
import SlipwayStore from "./slipwayStore";
import WaterStore from "./waterStore";

configure({ enforceActions: 'always' });

export class RootStore {
    userStore: UserStore;
    commonStore: CommonStore;
    modalStore: ModalStore;
    slipwayStore: SlipwayStore;
    waterStore: WaterStore;

    constructor() {
        this.userStore = new UserStore(this);
        this.commonStore = new CommonStore(this);
        this.modalStore = new ModalStore(this);
        this.slipwayStore = new SlipwayStore(this);
        this.waterStore = new WaterStore(this);
    }
}

export const RootStoreContext = createContext(new RootStore());
