package com.bvelop.slipways.ui.info

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.bvelop.slipways.BuildConfig
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.ui.LinkText

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InfoScreen() {
    Scaffold(topBar = { TopAppBar(title = { Text("Info") }) }) { padding ->
        Column(
            Modifier.padding(padding).verticalScroll(rememberScrollState()).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Text("Slipways zeigt Slipanlagen, Marinas und aktuelle Pegelstände an Gewässern in Deutschland und weiteren Ländern Europas.")
            LinkText("slipways.de", Links.WEBSITE)

            Heading("Datenquellen")
            Text(
                "Die meisten Slipanlagen und Marinas stammen aus OpenStreetMap. Daten © OpenStreetMap-Mitwirkende, " +
                    "verfügbar unter der Open Database License (ODbL).",
            )
            LinkText("openstreetmap.org/copyright", Links.OSM_COPYRIGHT)
            Text(
                "Pegel und Wasserstände: PEGELONLINE der Wasserstraßen- und Schifffahrtsverwaltung des Bundes (WSV), " +
                    "Datenlizenz Deutschland – Zero – Version 2.0. In den Niederlanden: Rijkswaterstaat (offene Daten), " +
                    "Wasserstand über NAP.",
            )
            LinkText("pegelonline.wsv.de", Links.PEGELONLINE)

            Heading("Lizenz")
            Text(
                "© 2019–2026 Marcel Benders. Quellcode unter der PolyForm Strict License 1.0.0. Nutzung über persönliche, " +
                    "nicht-kommerzielle Zwecke hinaus nur nach Rückfrage. Die Daten aus OpenStreetMap (ODbL) " +
                    "und PEGELONLINE (DL-DE->Zero-2.0) stehen unter ihren eigenen Lizenzen.",
            )
            LinkText("PolyForm Strict License 1.0.0", Links.LICENCE)
            LinkText("slipways@marcelbenders.com", Links.LICENCE_CONTACT)

            Heading("Rechtliches")
            LinkText("Impressum", Links.IMPRESSUM)
            LinkText("Datenschutz", Links.DATENSCHUTZ)
            LinkText("Support", Links.SUPPORT)

            Text(
                "Version ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun Heading(text: String) {
    Text(text, style = MaterialTheme.typography.titleMedium, modifier = Modifier.padding(top = 8.dp))
}
