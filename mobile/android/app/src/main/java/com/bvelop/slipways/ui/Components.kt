package com.bvelop.slipways.ui

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Public
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.LinkAnnotation
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextLinkStyles
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withLink
import androidx.compose.ui.unit.dp
import com.bvelop.slipways.shared.Countries
import com.bvelop.slipways.shared.CountryCount
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.NearestGauge
import com.bvelop.slipways.shared.PlaceInfo
import com.bvelop.slipways.shared.SourcedValue
import com.bvelop.slipways.shared.TickedFact
import com.bvelop.slipways.shared.WaterLevel

/** The ODbL attribution for entries from OpenStreetMap, linked to the copyright page. */
@Composable
fun OsmAttribution(modifier: Modifier = Modifier) {
    LinkText(Links.OSM_ATTRIBUTION, Links.OSM_COPYRIGHT, modifier, MaterialTheme.typography.labelSmall)
}

@Composable
fun LinkText(
    text: String,
    url: String,
    modifier: Modifier = Modifier,
    style: androidx.compose.ui.text.TextStyle = MaterialTheme.typography.bodyLarge,
) {
    val linkStyle = TextLinkStyles(SpanStyle(color = MaterialTheme.colorScheme.primary))
    Text(
        buildAnnotatedString { withLink(LinkAnnotation.Url(url, linkStyle)) { append(text) } },
        modifier = modifier,
        style = style,
    )
}

@Composable
fun WaterLevelText(level: WaterLevel, modifier: Modifier = Modifier) {
    val color = when (level.state) {
        "low" -> Color(0xFFE69500)
        "high" -> MaterialTheme.colorScheme.error
        "normal" -> Color(0xFF2E8B57)
        else -> MaterialTheme.colorScheme.onSurfaceVariant
    }
    Text(Format.waterLevel(level), modifier = modifier, color = color, fontWeight = FontWeight.SemiBold)
}

/** Label and value; nothing when the value is missing. */
@Composable
fun Fact(label: String, icon: ImageVector, value: String?, url: String? = null) {
    if (value.isNullOrBlank()) return
    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        Icon(icon, contentDescription = null, Modifier.size(20.dp), tint = MaterialTheme.colorScheme.primary)
        Column {
            Text(label, style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.SemiBold)
            if (url != null) LinkText(value, url) else Text(value, style = MaterialTheme.typography.bodyLarge)
        }
    }
}

@Composable
fun Centered(content: @Composable () -> Unit) {
    Box(Modifier.fillMaxSize().padding(16.dp), contentAlignment = Alignment.Center) { content() }
}

@Composable
fun LoadFailed(message: String, onRetry: () -> Unit) {
    Centered {
        Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text(message, color = MaterialTheme.colorScheme.error)
            Button(onClick = onRetry) { Text("Erneut versuchen") }
        }
    }
}

/** Chooses one country or all; hidden while there is nothing to choose between. */
@Composable
fun CountryMenu(options: List<CountryCount>, selected: String?, onSelect: (String?) -> Unit) {
    if (options.size < 2) return
    var expanded by remember { mutableStateOf(false) }
    Box {
        TextButton(onClick = { expanded = true }) {
            Icon(Icons.Filled.Public, contentDescription = null, Modifier.padding(end = 4.dp))
            Text(selected ?: "Alle", modifier = Modifier.semantics { contentDescription = "Land: ${selected?.let(Countries::name) ?: "alle Länder"}" })
        }
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            DropdownMenuItem(
                text = { Text("Alle Länder") },
                onClick = {
                    expanded = false
                    onSelect(null)
                },
            )
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option.label) },
                    onClick = {
                        expanded = false
                        onSelect(option.code)
                    },
                )
            }
        }
    }
}

/** Like [Fact], with where the value comes from when users or the place's website sent it. */
@Composable
fun SourcedFact(
    label: String,
    icon: ImageVector,
    value: SourcedValue?,
    display: (String) -> String = { it },
    link: ((String) -> String?)? = null,
) {
    if (value == null || value.value.isBlank()) return
    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        Icon(icon, contentDescription = null, Modifier.size(20.dp), tint = MaterialTheme.colorScheme.primary)
        Column {
            Text(label, style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.SemiBold)
            val url = link?.invoke(value.value)
            if (url != null) LinkText(display(value.value), url) else Text(display(value.value), style = MaterialTheme.typography.bodyLarge)
            value.note?.let { Note(it) }
        }
    }
}

@Composable
fun Note(text: String) {
    Text(text, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
}

@Composable
fun SectionTitle(text: String) {
    Text(text, style = MaterialTheme.typography.titleMedium)
}

/** Facts users ticked for a slipway, such as a jetty or a crane. */
@Composable
fun TickedFacts(facts: List<TickedFact>) {
    val first = facts.firstOrNull() ?: return
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        SectionTitle("Was gibt es dort?")
        facts.forEach { fact ->
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    if (fact.present) Icons.Filled.Check else Icons.Filled.Close,
                    contentDescription = null,
                    Modifier.size(18.dp),
                    tint = if (fact.present) Color(0xFF2E8B57) else MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Text(fact.label)
            }
        }
        Note("von Nutzern ergänzt, Stand ${Format.monthYear(first.retrieved)}")
    }
}

/** Tips from users below a slipway or marina. */
@Composable
fun Tips(tips: List<PlaceInfo>) {
    if (tips.isEmpty()) return
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        SectionTitle("Tipps")
        tips.forEach { tip ->
            Column {
                Text(tip.value)
                Note(Format.monthYear(tip.retrieved))
            }
        }
    }
}

/** The gauge with a current reading closest to a place, from the water the place is on. */
@Composable
fun NearestGaugeCard(gauge: NearestGauge, onClick: () -> Unit) {
    Card(Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text("Nächster Pegel: ${gauge.station.name}", style = MaterialTheme.typography.titleSmall)
            WaterLevelText(gauge.level)
            Text(
                "${Format.kilometres(gauge.km)} entfernt · gemessen ${Format.measuredAt(gauge.level)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}
