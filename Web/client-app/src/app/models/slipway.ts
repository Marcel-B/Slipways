export interface ISlipway {
    id: string;
    name: string;
    postalCode: string;
    city: string;
    street: string;
    country: string;
    latitude: number;
    longitude: number;
    water: string;
}
export interface ISlipwayFormValues extends Partial<ISlipway> {
    created?: Date;
}

export class SlipwayFormValues implements ISlipwayFormValues {
    id?: string = undefined;
    name: string = '';
    water: string = '';
    comment: string = '';
    created?: Date = undefined;
    city: string = '';
    country: string = '';
    street: string = '';
    postalCode: string = '';
    latitude: number = 0;
    longitude: number = 0;
    pro: string = '';
    contra: string = '';
    costs: number = -1;
    rating: number = 0;
    camping: boolean = false;
    parking: boolean = false;
    pier: boolean = true;

    constructor(init?: ISlipwayFormValues) {
        Object.assign(this, init);
    }
}
