package com.bvelop.slipways.ui.overview

import androidx.compose.foundation.clickable
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Marina
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.ui.map.PinColors
import com.bvelop.slipways.ui.marina.MarinaSheet

/** Marinas on the map; a tap opens the marina as a bottom sheet. */
@Composable
fun MarinaOverviewScreen(
    onWaterClick: (String) -> Unit,
    onSlipwayClick: (String) -> Unit,
    viewModel: PlaceOverviewViewModel<Marina> = viewModel(factory = PlaceOverviewViewModel.Marinas),
) {
    var selectedId by rememberSaveable { mutableStateOf<String?>(null) }

    PlaceOverviewScreen(
        title = "Marinas",
        singular = "Marina",
        plural = "Marinas",
        color = PinColors.MARINA,
        viewModel = viewModel,
        id = { it.id },
        position = { Position(it.latitude, it.longitude) },
        country = { it.country },
        onSelect = { selectedId = it.id },
    ) { marina ->
        ListItem(
            headlineContent = { Text(marina.name) },
            supportingContent = Format.place(marina.postalCode, marina.city)?.let { place -> @Composable { Text(place) } },
            modifier = Modifier.clickable { selectedId = marina.id },
        )
    }

    selectedId?.let { id ->
        MarinaSheet(id, onDismiss = { selectedId = null }, onWaterClick = onWaterClick, onSlipwayClick = onSlipwayClick)
    }
}
