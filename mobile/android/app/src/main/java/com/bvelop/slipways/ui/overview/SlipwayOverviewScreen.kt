package com.bvelop.slipways.ui.overview

import androidx.compose.foundation.clickable
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.shared.SlipwaySummary
import com.bvelop.slipways.ui.map.PinColors

@Composable
fun SlipwayOverviewScreen(
    onSlipwayClick: (String) -> Unit,
    onAdd: (MapArea) -> Unit,
    viewModel: PlaceOverviewViewModel<SlipwaySummary> = viewModel(factory = PlaceOverviewViewModel.Slipways),
) {
    PlaceOverviewScreen(
        title = "Slipanlagen",
        singular = "Slipanlage",
        plural = "Slipanlagen",
        color = PinColors.SLIPWAY,
        viewModel = viewModel,
        id = { it.id },
        position = { Position(it.latitude, it.longitude) },
        country = { it.country },
        onSelect = { onSlipwayClick(it.id) },
        onAdd = onAdd,
    ) { slipway ->
        ListItem(
            headlineContent = { Text(slipway.name) },
            supportingContent = slipway.subtitle.takeIf { it.isNotEmpty() }?.let { text -> @Composable { Text(text) } },
            modifier = Modifier.clickable { onSlipwayClick(slipway.id) },
        )
    }
}
