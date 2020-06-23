import React from 'react';
import { FieldRenderProps } from 'react-final-form';
import { FormFieldProps, Form, Label } from 'semantic-ui-react';

interface IProps
    extends FieldRenderProps<string , HTMLElement>,
        FormFieldProps {
    onChange: (value: string) => void;
}

const TextInput: React.FC<IProps> =
    ({
         input,
         width,
         type,
         placeholder,
         onChange,
         meta: { touched, error },
     }) => {
    return (
        <Form.Field
            error={touched && !!error}
            type={type}
            width={width}>
            <input
                {...input}
                onChange={(obj) => onChange(obj.target.value)}
                placeholder={placeholder}
            />
            {touched && error && (
                <Label basic color='red'>
                    {error}
                </Label>
            )}
        </Form.Field>
    );
};

export default TextInput;
