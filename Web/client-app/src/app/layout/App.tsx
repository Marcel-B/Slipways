import React, { Fragment } from 'react';
import { ToastContainer } from 'react-toastify';
import '../layout/Styles.css';
import { Route } from 'react-router-dom';
import HomePage from '../../features/home-page/HomePage';
import { Container } from 'semantic-ui-react';

    //   if (!appLoaded) return <LoadingComponent content='... loading app' />;

function App() {
    return (
        <Fragment>
            <ToastContainer position='bottom-right' />
            <Route exact path='/' component={HomePage} />
            <Route path={'/(.+)'} render={() => <Fragment>
                <Container style={{ marginTop: '7em' }}>
                    <Route exact path='/' component={HomePage}/>
                </Container>
            </Fragment>} />
        </Fragment>
    );
}

export default App;
