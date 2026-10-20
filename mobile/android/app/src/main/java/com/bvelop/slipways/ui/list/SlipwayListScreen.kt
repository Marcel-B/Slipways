package com.bvelop.slipways.ui.list

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.SlipwaySummary
import com.bvelop.slipways.ui.UiState

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlipwayListScreen(
    onSlipwayClick: (String) -> Unit,
    viewModel: SlipwayListViewModel = viewModel(factory = SlipwayListViewModel.Factory),
) {
    val query by viewModel.query.collectAsStateWithLifecycle()
    val state by viewModel.state.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(title = { Text("Slipanlagen") })
        },
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            OutlinedTextField(
                value = query,
                onValueChange = viewModel::onQueryChange,
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                placeholder = { Text("Nach Name, Ort oder Gewässer suchen") },
                leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
                singleLine = true,
            )
            when (val s = state) {
                UiState.Loading -> Centered { CircularProgressIndicator() }
                is UiState.Error -> Centered {
                    Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text("Slipanlagen konnten nicht geladen werden.", color = MaterialTheme.colorScheme.error)
                        Button(onClick = viewModel::retry) { Text("Erneut versuchen") }
                    }
                }
                is UiState.Success -> SlipwayList(s.data, onSlipwayClick)
            }
        }
    }
}

@Composable
private fun SlipwayList(slipways: List<SlipwaySummary>, onSlipwayClick: (String) -> Unit) {
    if (slipways.isEmpty()) {
        Centered { Text("Keine Slipanlagen gefunden.") }
        return
    }
    LazyColumn {
        items(slipways, key = { it.id }) { slipway ->
            ListItem(
                headlineContent = { Text(slipway.name) },
                supportingContent = {
                    Text(listOfNotNull(slipway.water.name, listOfNotNull(slipway.postalCode, slipway.city).joinToString(" ").ifBlank { null }).joinToString(" · "))
                },
                modifier = Modifier.clickable { onSlipwayClick(slipway.id) },
            )
            HorizontalDivider()
        }
    }
}

@Composable
private fun Centered(content: @Composable () -> Unit) {
    Box(Modifier.fillMaxSize().padding(16.dp), contentAlignment = Alignment.Center) { content() }
}
