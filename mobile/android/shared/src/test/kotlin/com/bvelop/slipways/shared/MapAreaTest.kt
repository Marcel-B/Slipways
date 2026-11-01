package com.bvelop.slipways.shared

import kotlin.test.Test
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class MapAreaTest {
    private val europe = MapArea(south = 35.0, west = -20.0, north = 62.0, east = 30.0)

    @Test
    fun keepsAViewThatOnlyChangedSize() {
        assertTrue(MapArea(south = 33.0, west = -25.0, north = 64.0, east = 35.0).sameView(4.0, europe, 4.0))
    }

    @Test
    fun tellsAPanOrAZoomApart() {
        assertFalse(MapArea(south = 35.0, west = -15.0, north = 62.0, east = 35.0).sameView(4.0, europe, 4.0))
        assertFalse(europe.sameView(5.0, europe, 4.0))
    }
}
