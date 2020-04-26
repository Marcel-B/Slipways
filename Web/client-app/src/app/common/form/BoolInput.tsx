import React from 'react';
import { FieldRenderProps } from 'react-final-form';
import {FormFieldProps, Form, Label, Checkbox, Container} from 'semantic-ui-react';
import {isNumeric} from "revalidate";

interface IProps
    extends FieldRenderProps<boolean , HTMLElement>,
        FormFieldProps {}

const BoolInput: React.FC<IProps> = ({
                                           input,
                checked,
                                           width,
                                          label,
                                           meta: { touched, error },
                                       }) => {
    return (
        <Form.Field error={touched && !!error} width={width} >
            <Checkbox label={label} style={{marginRight: 14}} checked={input.checked} onChange={input.onChange} value={input.value.toString()}/>
        </Form.Field>
    );
};

export default BoolInput;
