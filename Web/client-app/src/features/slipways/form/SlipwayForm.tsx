import React, {useContext, useEffect, useState} from "react";
import { RouteComponentProps } from 'react-router-dom';
import {RootStoreContext} from "../../../app/stores/rootStore";
import {observer} from "mobx-react-lite";
import {Button, Grid, Form, Segment, Checkbox, Header, Container} from "semantic-ui-react";
import {
    combineValidators,
    isRequired,
    composeValidators,
    hasLengthGreaterThan,
    hasLengthLessThan,
    hasLengthBetween,
    isNumeric
} from 'revalidate';
import {Form as FinalForm, Field} from 'react-final-form';
import TextInput from "../../../app/common/form/TextInput";
import SelectInput from "../../../app/common/form/SelectInput";
import TextAreaInput from "../../../app/common/form/TextAreaInput";
import {IWater} from "../../../app/models/water";
import {SlipwayFormValues} from "../../../app/models/slipway";
import NumberInput from "../../../app/common/form/NumberInput";
import BoolInput from "../../../app/common/form/BoolInput";

const validate = combineValidators({
    name: isRequired({ message: 'Der Name wird benötigt' }),
    water: isRequired('Gewässer'),
    country: composeValidators(
        isRequired({message: 'Land wird benötigt'}),
        hasLengthBetween(2, 2)({message: 'Darf nur eine Länge von zwei Buchstaben haben'}),
    )(),
    latitude: composeValidators(
        isRequired('Breitengrad'),
        isNumeric({message: 'Muss eine Nummer sein'})
    )(),
    longitude: composeValidators(
        isRequired('Längengrad'),
        isNumeric({message: 'Muss eine Nummer sein'})
    )(),
    costs: composeValidators(
        isRequired({message: 'Wird benötigt'}),
        isNumeric({message: 'Muss eine Zahl sein'})
    )(),
    rating: composeValidators(
        isRequired('Bewertung'),
        isNumeric('Muss eine Zahl sein'),
        hasLengthBetween(1, 1)({message: 'Zwischen 0 und 5'}))(),
    city: isRequired('Stadt / Ort'),
    street: isRequired('Straße'),
    postalcode: isRequired('PLZ')
});

interface DetailParams {
    id: string;
}


const SlipwayForm: React.FC<RouteComponentProps<DetailParams>> = ({
                                                                      match,
                                                                      history,
                                                                  }) => {
    const rootStore = useContext(RootStoreContext);
    const { waters, loadingWater, loadWaters } = rootStore.waterStore;
    const { loadSlipway} = rootStore.slipwayStore;

    const [slipway, setSlipway] = useState(new SlipwayFormValues());
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        loadWaters();
        if (match.params.id) {
            setLoading(true);
            loadSlipway(match.params.id)
                .then((slipway) =>
                    setSlipway(new SlipwayFormValues(slipway))
                )
                .finally(() => setLoading(false));
        }
    }, [loadSlipway, match.params.id, loadWaters]);


    const handleFinalFormSubmit = (values: any) => {
        console.log(values);
        // const dateAndTime = combineDateAndTime(values.date, values.time);
        // const { date, time, ...activity } = values;
        // // all properties of values without date and time are in activity now
        // activity.date = dateAndTime;
        // if (!activity.id) {
        //     let newActivity = {
        //         ...activity,
        //         id: uuid(),
        //     };
        //     createActivity(newActivity);
        // } else {
        //     editActivity(activity);
        // }
    };

    return (

        <Segment.Group>
            <Segment><Header as={'h2'}>Slipanlage</Header></Segment>

            <FinalForm
                validate={validate}
                onSubmit={handleFinalFormSubmit}
                render={({ handleSubmit, invalid, pristine }) => (
                    <Form onSubmit={handleSubmit}>
                        <Segment clearing>
                            <Grid columns={2} stackable>
                                <Grid.Column width={10}>
                                    <Field
                                        name='name'
                                        placeholder='Name'
                                        value={slipway.name}
                                        component={TextInput}
                                    />
                                </Grid.Column>

                                <Grid.Column width={6}>
                                    <Field
                                        name='country'
                                        placeholder='Land'
                                        value={slipway.country}
                                        component={TextInput}
                                    />
                                </Grid.Column>

                                <Grid.Column>
                                    <Field
                                        name='latitude'
                                        placeholder='Breitengrad'
                                        type={'number'}
                                        value={slipway.latitude}
                                        component={NumberInput}
                                    />
                                </Grid.Column>

                                <Grid.Column>
                                    <Field
                                        name='longitude'
                                        placeholder='Längengrad'
                                        type={'number'}
                                        value={slipway.longitude}
                                        component={NumberInput}
                                    />
                                </Grid.Column>

                                <Grid.Row columns={3}>
                                    <Grid.Column>
                                        <Field
                                            name='water'
                                            placeholder='Gewässer'
                                            component={SelectInput}
                                            value={slipway.water}
                                            options={waters.map((water: IWater) =>  {return {'key': water.id, 'text': water.name, 'value': water.id}})}
                                        />
                                    </Grid.Column>
                                    <Grid.Column>
                                        <Field
                                            name='costs'
                                            placeholder='Kosten'
                                            value={slipway.costs}
                                            component={NumberInput}
                                        />
                                    </Grid.Column>

                                    <Grid.Column>
                                        <Field
                                            name='rating'
                                            placeholder='Bewertung'
                                            value={slipway.rating}
                                            component={NumberInput}
                                        />
                                    </Grid.Column>
                                </Grid.Row>

                                <Grid.Row columns={3}>
                                    <Grid.Column>
                                        <Field
                                            name='street'
                                            placeholder='Straße'
                                            value={slipway.street}
                                            component={TextInput}
                                        />
                                    </Grid.Column>

                                    <Grid.Column>
                                        <Field
                                            name='postalcode'
                                            placeholder='PLZ'
                                            value={slipway.postalCode}
                                            component={TextInput}
                                        />
                                    </Grid.Column>

                                    <Grid.Column>
                                        <Field
                                            name='city'
                                            placeholder='Ort/Stadt'
                                            value={slipway.city}
                                            component={TextInput}
                                        />
                                    </Grid.Column>
                                </Grid.Row>
                                <Grid.Column>
                                    <Field
                                        name='pro'
                                        placeholder='Pro'
                                        value={slipway.pro}
                                        component={TextAreaInput}
                                        rows={2}
                                    />
                                </Grid.Column>
                                <Grid.Column>
                                    <Field
                                        name='contro'
                                        placeholder='Kontra'
                                        value={slipway.contra}
                                        component={TextAreaInput}
                                        rows={2}
                                    />
                                </Grid.Column>

                                <Grid.Column width={16}>
                                    <Field
                                        name='comment'
                                        placeholder='Kommentar'
                                        rows={3}
                                        value={slipway.comment}
                                        component={TextAreaInput}
                                    />
                                </Grid.Column>
                            </Grid>

                            <Container style={{marginTop: 14}}>
                                <Field
                                    name='camp'
                                    checked={slipway.camping}
                                    value={slipway.camping}
                                    label={'Campingplatz'}
                                    component={BoolInput}
                                />
                                <Field
                                    name='pier'
                                    checked={slipway.pier}
                                    value={slipway.pier}
                                    label={'Anleger / Steg'}
                                    component={BoolInput}
                                />
                                <Checkbox label='Parkplatz' style={{marginRight: 14}} checked={slipway.parking}/>
                                <Button
                                    onClick={handleSubmit}
                                    floated='right'
                                    disabled={invalid || pristine}
                                    positive
                                    type='submit'
                                    content='Speichern'
                                />
                                <Button
                                    onClick={
                                        slipway!.id
                                            ? () =>
                                                history.push(
                                                    `/slipways/${slipway!.id}`
                                                )
                                            : () => history.push('/slipways')
                                    }
                                    floated='right'
                                    disabled={loading}
                                    type='submit'
                                    content='Abbruch'
                                />
                            </Container>
                        </Segment>
                    </Form>
                )}
            />
        </Segment.Group>

    );
};

export default observer(SlipwayForm);
