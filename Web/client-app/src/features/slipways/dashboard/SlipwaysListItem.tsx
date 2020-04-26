import React from "react";
import {Table, Item, Segment} from "semantic-ui-react";
import {ISlipway} from "../../../app/models/slipway";
import {observer} from "mobx-react-lite";

const SlipwaysListItem: React.FC<{slipway: ISlipway}> = ({slipway}) => {
    return(
        <Table.Row>
            <Table.Cell>{slipway.name}</Table.Cell>
            <Table.Cell>{`${slipway.street}`}</Table.Cell>
            <Table.Cell>{`${slipway.postalCode} ${slipway.city}`}</Table.Cell>
            <Table.Cell>{slipway.water}</Table.Cell>
        </Table.Row>
    )
};

export default observer(SlipwaysListItem);
