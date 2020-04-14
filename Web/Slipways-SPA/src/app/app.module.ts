import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { AppComponent } from './app.component';
import { AdminSlipwaysComponent } from './admin/admin-slipways/admin-slipways.component';
import { SlipwayListComponent } from './slipways/slipway-list/slipway-list.component';
import { WaterListComponent } from './waters/water-list/water-list.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NavComponent } from './nav/nav.component';
import {MatToolbarModule} from '@angular/material/toolbar';
import {MatIconModule} from '@angular/material/icon';
import {MatInputModule} from '@angular/material/input';
import {MatTabsModule} from '@angular/material/tabs';
import {MatGridListModule} from '@angular/material/grid-list';
import {SlipwayService} from './_services/slipway.service';
import {HttpClientModule} from '@angular/common/http';
import {MatTableModule} from '@angular/material/table';
import {MatButton, MatButtonModule} from '@angular/material/button';

@NgModule({
  declarations: [
    AppComponent,
    AdminSlipwaysComponent,
    SlipwayListComponent,
    WaterListComponent,
    NavComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    MatButtonModule,
    MatToolbarModule,
    MatIconModule,
    MatInputModule,
    MatTabsModule,
    MatGridListModule,
    MatTableModule
  ],
  providers: [
    SlipwayService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
