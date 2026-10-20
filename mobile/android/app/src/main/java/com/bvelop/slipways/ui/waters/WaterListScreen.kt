package com.bvelop.slipways.ui.waters

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.WaterSummary
import com.bvelop.slipways.ui.Centered
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.WaterLevelText

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WaterListScreen(
    onWaterClick: (String) -> Unit,
    viewModel: WaterListViewModel = viewModel(factory = WaterListViewModel.Factory),
) {
    val query by viewModel.query.collectAsStateWithLifecycle()
    val state by viewModel.state.collectAsStateWithLifecycle()

    Scaffold(topBar = { TopAppBar(title = { Text("Gewässer") }) }) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            OutlinedTextField(
                value = query,
                onValueChange = viewModel::onQueryChange,
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                placeholder = { Text("Gewässer suchen") },
                leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
                singleLine = true,
            )
            when (val s = state) {
                UiState.Loading -> Centered { CircularProgressIndicator() }
                is UiState.Error -> LoadFailed("Gewässer konnten nicht geladen werden.", viewModel::retry)
                is UiState.Success -> WaterList(s.data, onWaterClick)
            }
        }
    }
}

@Composable
private fun WaterList(waters: List<WaterSummary>, onWaterClick: (String) -> Unit) {
    if (waters.isEmpty()) {
        Centered { Text("Keine Gewässer gefunden.") }
        return
    }
    LazyColumn {
        items(waters, key = { it.id }) { water ->
            ListItem(
                headlineContent = { Text(water.name) },
                supportingContent = {
                    Text(
                        listOf(
                            Format.count(water.slipwayCount, "Slipanlage", "Slipanlagen"),
                            Format.count(water.stationCount, "Pegel", "Pegel"),
                        ).joinToString(" · "),
                    )
                },
                trailingContent = water.gauge?.let { gauge -> @Composable { WaterLevelText(gauge.waterLevel) } },
                modifier = Modifier.clickable { onWaterClick(water.id) },
            )
            HorizontalDivider()
        }
    }
}
