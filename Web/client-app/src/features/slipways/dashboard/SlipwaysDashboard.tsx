import {observer} from "mobx-react-lite";
import React, {Fragment, useContext, useState} from "react";
import {Button, Form, Grid, Header} from "semantic-ui-react";
import SlipwaysList from "./SlipwaysList";
import {RootStoreContext} from "../../../app/stores/rootStore";

const SlipwaysDashboard: React.FC = () => {
    const rootStore = useContext(RootStoreContext);
    const {slipways, loadSlipways, reset} = rootStore.slipwayStore;
    const [currentSearch, setCurrentSearch] = useState("");

    const dooFoo = async (e: any) => {
        setCurrentSearch(e.target.value);
        await loadSlipways(e.target.value);
    }

    return (
        <Fragment>
            <Header as='h1' content='Slipanlagen' textAlign={"center"}/>

            <Grid columns={2} stackable>

                <Grid.Column width={14}>
                    <Form>
                        <input name='search'
                               value={currentSearch}
                               onChange={dooFoo}
                               autoFocus
                               placeholder='Suche - Name, Adresse, Ort oder Gewässer hier eintippen'/>
                    </Form>
                </Grid.Column>

                <Grid.Column width={2}>
                    <Button size='tiny'
                            inverted
                            onClick={() => {
                                reset();
                                setCurrentSearch("");
                            }}>
                        reset
                    </Button>
                </Grid.Column>
            </Grid>
            <SlipwaysList slipways={slipways}/>
        </Fragment>)
};

export default observer(SlipwaysDashboard);
