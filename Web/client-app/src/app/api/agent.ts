import axios, { AxiosResponse } from 'axios';
import { ISlipway } from '../models/slipway';
import {IUser} from "../models/user";
import {IUserFormValues} from "../models/userFormValues";
import {IWater} from "../models/water";

axios.defaults.baseURL = process.env.REACT_APP_API_URL!;// 'http://localhost:5000/api';

const responseBody = (response: AxiosResponse) => response.data;

const requests = {
    get: (url: string) => axios.get(url).then(responseBody),
    post: (url: string, body: {}) => axios.post(url, body).then(responseBody),
};

const Slipways = {
    list: (): Promise<ISlipway[]> => requests.get('/slipways'),
    details: (id: string): Promise<ISlipway> => requests.get(`slipways/${id}`),
};

const Waters = {
    list: (): Promise<IWater[]> => requests.get('/waters')
};
const User = {
    current: (): Promise<IUser> => requests.get('/user'),
    login: (user: IUserFormValues): Promise<IUser> => requests.post(`/user/login`, user),
    register: (user: IUserFormValues): Promise<IUser> => requests.post(`/user/register`, user)
}

export default { Slipways , User, Waters};
