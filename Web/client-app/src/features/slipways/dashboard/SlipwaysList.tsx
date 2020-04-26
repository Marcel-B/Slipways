import React, {Fragment} from "react";
import {Table} from "semantic-ui-react";
import SlipwaysListItem from "./SlipwaysListItem";
import {ISlipway} from "../../../app/models/slipway";
import {observer} from "mobx-react-lite";

const SlipwaysList: React.FC<{slipways: ISlipway[]}> = ({slipways}) => {
    return (
        <Fragment>
                <Table basic='very' celled collapsing>
                    <Table.Header>
                        <Table.Row>
                            <Table.HeaderCell>Name</Table.HeaderCell>
                            <Table.HeaderCell>Adresse</Table.HeaderCell>
                            <Table.HeaderCell>Ort</Table.HeaderCell>
                            <Table.HeaderCell>Gewässer</Table.HeaderCell>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {slipways.map(slipway => (
                            <SlipwaysListItem slipway={slipway} key={slipway.id}/>
                        ))}
                    </Table.Body>
                </Table>
        </Fragment>)
};

export default observer(SlipwaysList);
