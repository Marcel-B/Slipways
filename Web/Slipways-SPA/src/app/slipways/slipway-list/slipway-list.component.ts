import { Component, OnInit } from '@angular/core';
import {SlipwayService} from '../../_services/slipway.service';

class Slipways {
}

@Component({
  selector: 'app-slipway-list',
  templateUrl: './slipway-list.component.html',
  styleUrls: ['./slipway-list.component.css']
})
export class SlipwayListComponent implements OnInit {
 slipways: Slipways[];

  constructor(private slipwayService: SlipwayService) { }

  ngOnInit(): void {
    this.slipwayService.getSlipwaysByFilter('Test').subscribe(result => {
      this.slipways = result;
      console.log(result);
    });
  }

  rowClicked(test: string){
   console.log(test);
  }
}
