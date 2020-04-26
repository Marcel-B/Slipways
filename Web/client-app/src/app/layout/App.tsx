import React, { Fragment } from 'react';
import { ToastContainer } from 'react-toastify';
import '../layout/Styles.css';
import HomePage from '../../features/home-page/HomePage';
import { Container } from 'semantic-ui-react';
import NavBar from "../../features/nav/NavBar";
import SlipwaysDashboard from "../../features/slipways/dashboard/SlipwaysDashboard";
import SlipwayForm from "../../features/slipways/form/SlipwayForm";
import { Route, withRouter} from 'react-router-dom';
import {observer} from "mobx-react-lite";

    //   if (!appLoaded) return <LoadingComponent content='... loading app' />;

function App() {
    return (
        <Fragment>
            <ToastContainer position='bottom-right' />
            <Route exact path='/' component={HomePage} />
            <Route path={'/(.+)'} render={() => <Fragment>
                <NavBar/>
                <Container style={{ marginTop: '7em' }}>
                    <Route exact path='/slipways' component={SlipwaysDashboard}/>
                    <Route exact path='/slipways/create' component={SlipwayForm}/>
                </Container>
            </Fragment>} />
        </Fragment>
    );
}

export default withRouter(observer(App));
