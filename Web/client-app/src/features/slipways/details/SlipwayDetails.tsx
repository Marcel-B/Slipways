import {observer} from "mobx-react-lite";
import React, { Fragment } from "react";
import {Link, RouteComponentProps} from "react-router-dom";
import {Button} from "semantic-ui-react";

interface DetailParams {
    id: string;
}
const SlipwayDetails: React.FC<RouteComponentProps<DetailParams>> = ({match, history}) => {
    return (
        <Fragment>
            <Button as={Link} to='/slipways'>Zurück</Button>
            <div>The id is '{match.params.id}'</div>
        </Fragment>
    )
};

export default observer(SlipwayDetails);

