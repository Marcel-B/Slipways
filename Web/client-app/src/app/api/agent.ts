import axios, { AxiosResponse } from 'axios';
import { ISlipway } from '../models/slipway';
import {IUser} from "../models/user";

axios.defaults.baseURL = 'http://localhost:5000/api';

const responseBody = (response: AxiosResponse) => response.data;

const requests = {
    get: (url: string) => axios.get(url).then(responseBody),
    post: (url: string, body: {}) => axios.post(url, body).then(responseBody),
};

const Slipways = {
    list: (): Promise<ISlipway[]> => requests.get('/slipways')
};

const User = {
    login: (email: string, password: string): Promise<IUser> => requests.post('/user/login', {email, password})
};

export default { Slipways , User};
