import React from 'react';

import {
    Icon,
    Header,
    Image,
    Divider,
    Button,
    Segment,
    Container,
    Transition,
} from 'semantic-ui-react';
import { Link } from 'react-router-dom';
import TextInput from "../../app/common/form/TextInput";

const HomePage: React.FC = () => {

    const handleFinalFormSubmit = (values: any) => {

    };

    let handleSubmit;
    return (
        <Segment inverted textAlign='center' vertical className='masthead'>
            <Container fluid>
                <h1 style={{ fontSize: '4em' }}>
                    <Icon
                        flipped='horizontally'
                        name='ship'
                        size='big'
                        style={{ marginRight: '18px' }}
                    />
                    Slipways.de
                    <Icon
                        name='ship'
                        size='big'
                        style={{ marginLeft: '18px' }}
                    />
                </h1>
                    <a className="align-middle"
                        href="https://apps.apple.com/de/app/slipways/id1484222697?mt=8"
                        style={{
                            display: 'inline-block',
                            overflow: 'hidden',
                            background: 'url(https://linkmaker.itunes.apple.com/de-de/badge-lrg.svg?releaseDate=2019-12-14&kind=iossoftware&bubble=ios_apps) no-repeat',
                            width: '135px',
                            height: '40px'
                        }}/>
                <Divider />

                <Header as='h2'>
                    Eine kleine Sammlung von Slipanlagen welche ständig erweitert wird. Durch einfachen Knopfdruck dann die Navigation gestartet werden.
                    <br/>
                    <br/>
                    Lass dein Boot ins Wasser
                    <br />
                    <br />
                    <Transition.Group animation='scale' duration={1200}>
                        <Image
                            src={'/assets/boat_launch_100.svg'}
                            centered
                            size='huge'
                        />
                    </Transition.Group>
                </Header>
                <Button as={Link} to='/slipways' size='huge' inverted>
                    weiter
                </Button>
            </Container>
        </Segment>
    );
};

export default HomePage;
