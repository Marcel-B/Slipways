import React from 'react';
import { Form as FinalForm, Field } from 'react-final-form';

import {
    Icon,
    Header,
    Image,
    Divider,
    Button,
    Segment,
    Container,
    Transition, Form,
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
                        name='space shuttle'
                        size='big'
                        style={{ marginRight: '18px' }}
                    />
                    Slipways
                    <Icon
                        name='space shuttle'
                        size='big'
                        style={{ marginLeft: '18px' }}
                    />
                </h1>

                <Divider />

                <Header as='h2'>
                    Let your boat to water
                    <br />
                    <br />
                    <Transition.Group animation='scale' duration={1200}>
                        <Image
                            src={'/assets/robot_100.png'}
                            centered
                            size='huge'
                        />
                    </Transition.Group>
                </Header>
                <FinalForm onSubmit={handleFinalFormSubmit}
                           render={() => (
                               <Form>
                                   <Field
                                       name='title'
                                       placeholder='Title'
                                       value=''
                                       component={TextInput}
                                   />
                               </Form>
                           )}
                    />
                <Button as={Link} to='/images' size='huge' inverted>
                    To Slipways
                </Button>
            </Container>
        </Segment>
    );
};

export default HomePage;
