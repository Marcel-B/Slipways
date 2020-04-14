import { Injectable } from '@angular/core';
import {Slipway} from '../_models/slipway';
import {HttpClient, HttpParams} from '@angular/common/http';
import {environment} from '../../environments/environment';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SlipwayService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getSlipways() {
      return this.http.get<Slipway[]>(this.baseUrl + 'slipways');
  }

  getSlipwaysByFilter(filter: string) {
    let params = new HttpParams();
    params = params.append('filter', filter);
    return this.http.get<Slipway[]>(this.baseUrl + 'slipways', {observe: 'body', params });
  }
}
