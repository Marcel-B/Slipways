import {observer} from "mobx-react-lite";
import React, {Fragment, useContext, useEffect} from "react";
import {Header} from "semantic-ui-react";
import SlipwaysList from "./SlipwaysList";
import {RootStoreContext} from "../../../app/stores/rootStore";
import {LoadingComponent} from "../../../app/layout/LoadingComponent";

const SlipwaysDashboard: React.FC = () => {
    const rootStore = useContext(RootStoreContext);
    const {loadingSlipways, slipways, loadSlipways} = rootStore.slipwayStore;

    useEffect(() => {
        loadSlipways();
    }, [loadSlipways]);


    if (loadingSlipways)
        return <LoadingComponent content='Lade Slipanlagen ...' />;
    return (
        <Fragment>
            <Header as='h1' content='Slipanlagen' textAlign={"center"}/>
            <SlipwaysList slipways={slipways}/>
        </Fragment>)
};

export default  observer(SlipwaysDashboard);
