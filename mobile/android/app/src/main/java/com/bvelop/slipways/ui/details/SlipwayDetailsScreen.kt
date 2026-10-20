package com.bvelop.slipways.ui.details

import android.content.ActivityNotFoundException
import android.content.Intent
import android.net.Uri
import android.widget.Toast
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Map
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.net.toUri
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.SlipwayDetails
import com.bvelop.slipways.ui.UiState
import java.text.NumberFormat
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlipwayDetailsScreen(
    onBack: () -> Unit,
    viewModel: SlipwayDetailsViewModel = viewModel(factory = SlipwayDetailsViewModel.Factory),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text((state as? UiState.Success)?.data?.name ?: "Slipanlage") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Zurück")
                    }
                },
            )
        },
    ) { padding ->
        Box(Modifier.padding(padding).fillMaxSize()) {
            when (val s = state) {
                UiState.Loading -> CircularProgressIndicator(Modifier.align(Alignment.Center))
                is UiState.Error -> Column(
                    Modifier.align(Alignment.Center).padding(16.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Text(s.message, color = MaterialTheme.colorScheme.error)
                    Button(onClick = viewModel::load) { Text("Erneut versuchen") }
                }
                is UiState.Success -> Details(s.data)
            }
        }
    }
}

private val currency: NumberFormat = NumberFormat.getCurrencyInstance(Locale.GERMANY)

@Composable
private fun Details(slipway: SlipwayDetails) {
    val context = LocalContext.current

    Column(
        Modifier.verticalScroll(rememberScrollState()).padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Fact("Gewässer", slipway.water.name)
        if (slipway.address.isNotBlank()) Fact("Adresse", slipway.address)
        Fact(
            "Kosten",
            when (val costs = slipway.costs) {
                null -> "unbekannt"
                0.0 -> "kostenlos"
                else -> currency.format(costs)
            },
        )
        slipway.marina?.let { Fact("Marina", it.name) }
        if (slipway.extras.isNotEmpty()) Fact("Extras", slipway.extras.joinToString(", "))
        Fact("Koordinaten", String.format(Locale.ROOT, "%.5f, %.5f", slipway.latitude, slipway.longitude))
        slipway.comment?.let { Text(it, style = MaterialTheme.typography.bodyLarge) }

        Button(
            onClick = {
                val label = Uri.encode(slipway.name)
                val uri = "geo:0,0?q=${slipway.latitude},${slipway.longitude}($label)".toUri()
                try {
                    context.startActivity(Intent(Intent.ACTION_VIEW, uri))
                } catch (_: ActivityNotFoundException) {
                    Toast.makeText(context, "Keine Karten-App installiert.", Toast.LENGTH_SHORT).show()
                }
            },
            contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
        ) {
            Icon(Icons.Filled.Map, contentDescription = null, Modifier.padding(end = 8.dp))
            Text("In Karten öffnen")
        }
    }
}

@Composable
private fun Fact(label: String, value: String) {
    Column {
        Text(label, style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.SemiBold)
        Text(value, style = MaterialTheme.typography.bodyLarge)
    }
}
